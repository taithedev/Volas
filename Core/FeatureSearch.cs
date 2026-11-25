using System;
using System.Collections.Generic;
using System.Text;

namespace Volas
{
    // Shared, deterministic search. Indexes immutable feature metadata once.
    internal sealed class FeatureSearch
    {
        private readonly Dictionary<Feature, string[]> index = new Dictionary<Feature, string[]>();
        private static readonly Dictionary<string,string> aliases = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase)
        {
            {"guns","gun"}, {"pointer","gun"}, {"reticle","pointer gun"}, {"esp","marker esp"},
            {"fps","fps frame rate"}, {"ping","ping latency"}, {"cosmetics","visuals charms"},
            {"charm","charms"}, {"favorites","favorite saved"}, {"hud","hud display"},
            {"projectiles","gun mods"}, {"crosshair","crosshair reticle"}
        };

        internal static string Normalize(string value)
        {
            var b=new StringBuilder();
            bool space=true;
            foreach(char c in value??"")
            {
                if(char.IsLetterOrDigit(c)) { b.Append(char.ToLowerInvariant(c)); space=false; }
                else if(!space) { b.Append(' '); space=true; }
            }
            return b.ToString().Trim();
        }

        private string[] Fields(Feature f)
        {
            string[] fields;
            if(!index.TryGetValue(f,out fields))
            {
                string extras=f.Category=="Gun Mods"?"pointer projectiles tools":f.Category=="Charms"?"cosmetics wrist accessories":f.Category=="Telemetry"?"hud display statistics":"";
                fields=new[]{Normalize(f.Label),Normalize(f.Id),Normalize(f.Category),Normalize(f.Description),extras};
                index[f]=fields;
            }
            return fields;
        }

        private static int ScoreTerm(string term,string[] fields)
        {
            int best=0;
            string alternatives;
            var words=aliases.TryGetValue(term,out alternatives)?(term+" "+alternatives).Split(' '):new[]{term};
            foreach(string word in words)
            {
                if(word.Length==0)continue;
                for(int i=0;i<fields.Length;i++)
                {
                    string field=fields[i];
                    if(field==word)best=Math.Max(best,i==0?30:15);
                    else if(field.StartsWith(word+" ",StringComparison.Ordinal))best=Math.Max(best,i==0?23:12);
                    else if(field.IndexOf(word,StringComparison.Ordinal)>=0)best=Math.Max(best,i==0?18:i==1?14:5);
                }
            }
            return best;
        }

        internal List<Feature> Find(IEnumerable<Feature> source,string query,Predicate<Feature> include=null)
        {
            string normalized=Normalize(query);
            var result=new List<Feature>();
            if(normalized.Length==0)
            {
                foreach(var f in source)if(include==null||include(f))result.Add(f);
                return result;
            }
            var terms=normalized.Split(new[]{' '},StringSplitOptions.RemoveEmptyEntries);
            var scores=new Dictionary<Feature,int>();
            foreach(var f in source)
            {
                if(include!=null&&!include(f))continue;
                var fields=Fields(f);
                int score=0;
                foreach(string term in terms)
                {
                    int v=ScoreTerm(term,fields);
                    if(v==0){score=-1;break;}
                    score+=v;
                }
                if(score<0)continue;
                if(fields[0]==normalized)score+=100;
                else if(fields[0].StartsWith(normalized,StringComparison.Ordinal))score+=40;
                else if(fields[0].IndexOf(normalized,StringComparison.Ordinal)>=0)score+=20;
                scores[f]=score;result.Add(f);
            }
            result.Sort((a,b)=>{int c=scores[b].CompareTo(scores[a]);return c!=0?c:StringComparer.OrdinalIgnoreCase.Compare(a.Label,b.Label);});
            return result;
        }
    }
}
