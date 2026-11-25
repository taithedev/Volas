import {createHash,randomBytes} from "node:crypto";
const hash=s=>createHash("sha256").update(s,"utf8").digest("hex");
const response=(s,status=200)=>new Response(s,{status,headers:{"content-type":"text/plain; charset=utf-8","cache-control":"no-store"}});
const query=async (query,params=[])=>{
  const d=process.env.DATABASE_URL;
  if(!d)throw new Error("Database unavailable");
  const u=new URL(d);
  const r=await fetch("https://"+u.hostname+"/sql",{method:"POST",headers:{"content-type":"application/json","Neon-Connection-String":d,"Neon-Raw-Text-Output":"true","Neon-Array-Mode":"false"},body:JSON.stringify({query,params}),signal:AbortSignal.timeout(8000)});
  if(!r.ok)throw new Error("Database query failed");
  const data=await r.json();
  return data.rows??[];
};
const base64=s=>Buffer.from(s??"","utf8").toString("base64");
let windowStart=0, attempts=0;
function limit(){const now=Date.now();if(now-windowStart>60000){windowStart=now;attempts=0;}return ++attempts<=180;}
async function latest(){const rows=await query("SELECT id::text,body FROM volas.announcements ORDER BY created_at DESC LIMIT 1");return rows[0]??{id:"",body:""};}
async function summary(role){
  if(role!=="owner")return ["0","0","0"];
  const [row]=await query("SELECT (SELECT count(*) FROM volas.licenses WHERE revoked_at IS NULL)::text AS licenses,(SELECT count(*) FROM volas.licenses WHERE revoked_at IS NOT NULL)::text AS revoked,(SELECT count(DISTINCT license_id) FROM volas.sessions WHERE share_presence AND expires_at>now() AND last_seen>now()-interval '2 minutes')::text AS active");
  return [row.licenses,row.revoked,row.active];
}
async function output(role,token=""){
  const a=await latest(),s=await summary(role);
  return response(["OK",role,token,a.id,base64(a.body),...s].join("\n"));
}
export default {async fetch(req){
  if(req.method==="GET"&&new URL(req.url).pathname==="/health")return response("Volas licensing API");
  if(req.method!=="POST")return response("Method not allowed",405);
  if(!limit())return response("Try again shortly",429);
  const action=req.headers.get("x-volas-action")??"";
  if(!["login","heartbeat","announce","logout"].includes(action))return response("Bad action",400);
  if(Number(req.headers.get("content-length")??0)>2048)return response("Request too large",413);
  try{
    const body=await req.text();
    if(body.length>2048)return response("Request too large",413);
    const presence=req.headers.get("x-volas-presence")==="true";
    if(action==="login"){
      const key=body.trim().toUpperCase();
      if(!/^VOLAS-[A-F0-9]{64}$/.test(key))return response("Invalid license",401);
      const token=randomBytes(32).toString("hex");
      const rows=await query("INSERT INTO volas.sessions(token_hash,license_id,expires_at,last_seen,share_presence,version) SELECT $1,id,now()+interval '12 hours',CASE WHEN $3::boolean THEN now() ELSE NULL END,$3::boolean,'1.6.0' FROM volas.licenses WHERE key_hash=$2 AND revoked_at IS NULL RETURNING license_id::text",[hash(token),hash(key),String(presence)]);
      if(!rows.length)return response("Invalid or revoked license",401);
      const [license]=await query("SELECT role FROM volas.licenses WHERE id=$1::uuid AND revoked_at IS NULL",[rows[0].license_id]);
      if(!license)return response("Invalid or revoked license",401);
      return output(license.role,token);
    }
    const auth=req.headers.get("authorization")??"";
    if(!/^Bearer [a-f0-9]{64}$/.test(auth))return response("Authentication required",401);
    const session=hash(auth.slice(7));
    const [user]=await query("SELECT l.id::text,l.role FROM volas.sessions s JOIN volas.licenses l ON l.id=s.license_id WHERE s.token_hash=$1 AND s.expires_at>now() AND l.revoked_at IS NULL",[session]);
    if(!user)return response("Session expired or revoked",401);
    if(action==="logout"){await query("UPDATE volas.sessions SET expires_at=now(),last_seen=NULL,share_presence=false WHERE token_hash=$1",[session]);return response("OK");}
    if(action==="announce"){
      if(user.role!=="owner")return response("Forbidden",403);
      const message=body.trim();
      if(message.length<1||message.length>500)return response("Announcement must be 1-500 characters",400);
      await query("WITH a AS (INSERT INTO volas.announcements(body) VALUES ($1) RETURNING id) INSERT INTO volas.owner_audit(actor,action) SELECT $2::uuid,'announcement:'||id::text FROM a",[message,user.id]);
    }
    await query("UPDATE volas.sessions SET share_presence=$2::boolean,last_seen=CASE WHEN $2::boolean THEN now() ELSE NULL END WHERE token_hash=$1",[session,String(presence)]);
    return output(user.role);
  }catch{return response("Service unavailable",503);}
}};