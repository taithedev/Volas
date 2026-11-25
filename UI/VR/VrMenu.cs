using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

namespace Volas
{
    // A compact wrist interface with a persistent world-space canvas.
    // UI and interaction use the same local coordinate system.
    internal sealed class VrMenu
    {
        #region Layout, state & palette

        private const float Unit = .0005f;
        private const float Width = 480f, Height = 640f;
        private const int PerPage = 4;
        private readonly VolasPlugin plugin;
        private readonly List<Slot> slots = new List<Slot>(16);
        private readonly List<InputDevice> devices = new List<InputDevice>(4);
        private readonly List<Text> labels = new List<Text>();
        private readonly List<int> fontSizes = new List<int>();
        private readonly Slot[] rows = new Slot[PerPage], favorites = new Slot[PerPage];
        private Slot home, saved, enabled, all, previous, next, local, reset;
        private GameObject root, dot;
        private RectTransform panel;
        private LineRenderer beam;
        private Material pointerMaterial;
        private Font font;
        private Text heading, pageLabel, feedback, fps;
        private int page, pageCount = 1, revision = -1, hovered = -1;
        private bool dirty = true, wasOpen, oldTrigger, touchReady;
        private float nextPress, nextStats, messageUntil, lastFontScale = -1f;
        private string category = "", message = "";
        private enum Mode { Local, Home, Category, Saved, Enabled, All }
        private Mode mode = Mode.Local;
        private bool roomAllowed;
        private float nextRoomCheck;
        private bool lastPreview;
        private bool hasPose;
        internal string Status { get; private set; } = "VR menu ready";

        private static readonly Color Black = new Color32(10,10,12,255);
        private static readonly Color Surface = new Color32(35,35,39,255);
        private static readonly Color Gray = new Color32(66,66,72,255);
        private static readonly Color White = new Color32(248,248,250,255);
        private static readonly Color Muted = new Color32(181,181,190,255);
        private static readonly Color Ink = new Color32(15,15,18,255);

        private sealed class Slot
        {
            internal Rect Area;
            internal Image Image;
            internal Text Text;
            internal Action Action;
            internal bool Selected, Available;
        }

        #endregion

        #region Initialization & frame update

        internal VrMenu(VolasPlugin owner) { plugin = owner; }

        internal void Tick()
        {
            if (!plugin.VrOpen)
            {
                if (root != null && root.activeSelf) root.SetActive(false);
                if (wasOpen) { HidePointer(); dirty = true; wasOpen = false; hovered = -1; hasPose = false; }
                Status = "Closed | Y or F7";
                return;
            }
            Camera camera = plugin.Game.HeadCamera;
            if (camera == null)
            {
                if (root != null) root.SetActive(false);
                wasOpen = false;
                Status = "Waiting for camera";
                return;
            }
            if (root == null) Create();
            if (!root.activeSelf) root.SetActive(true);
            Transform head = camera.transform;
            Transform left = plugin.Game.LeftHand;
            Vector3 pos = head.position + head.forward*.52f - head.right*.055f - head.up*.025f;
            bool preview = true;
            if (left != null)
            {
                Vector3 candidate = left.TransformPoint(new Vector3(plugin.Settings.VrOffsetX.Value,
                    plugin.Settings.VrOffsetY.Value, plugin.Settings.VrOffsetZ.Value));
                Vector3 d = candidate-head.position;
                if (d.sqrMagnitude >= .24f*.24f && d.sqrMagnitude < 1.8f*1.8f &&
                    Vector3.Dot(head.forward,d.normalized) >= .12f)
                { pos = candidate; preview = false; }
            }
            // Canvas front is -Z; point +Z away from the viewer to avoid reversed labels.
            Vector3 facing = pos-head.position;
            Quaternion desired = facing.sqrMagnitude > .0001f ? Quaternion.LookRotation(facing.normalized,head.up) : head.rotation;
            if (!hasPose || preview != lastPreview) root.transform.SetPositionAndRotation(pos, desired);
            else
            {
                float blend = 1f - Mathf.Exp(-28f * Time.unscaledDeltaTime);
                root.transform.SetPositionAndRotation(Vector3.Lerp(root.transform.position,pos,blend), Quaternion.Slerp(root.transform.rotation,desired,blend));
            }
            hasPose = true; lastPreview = preview;
            root.transform.localScale = Vector3.one * plugin.Settings.VrScale.Value *
                (plugin.Compact ? .82f : 1f);
            Status = preview ? "Camera preview" : "Wrist menu active";

            InputDevice controller = RightDevice();
            bool trigger = ReadTrigger(controller);
            float analog;
            if (plugin.Game.TryControllerFloat("rightControllerIndexFloat", out analog) && analog >= .65f)
                trigger = true;
            if (!wasOpen)
            {
                wasOpen = true; dirty = true; oldTrigger = trigger; touchReady = false;
                nextPress = Time.unscaledTime+.18f;
            }
            if (Time.unscaledTime >= nextRoomCheck)
            {
                nextRoomCheck = Time.unscaledTime + .35f;
                bool allowed = plugin.Game.IsSoloOrModdedRoom();
                if (allowed != roomAllowed) { roomAllowed = allowed; dirty = true; }
            }
            if (dirty || revision != plugin.Features.Revision) Refresh();
            UpdateFont();
            if (Time.unscaledTime >= nextStats)
            {
                nextStats = Time.unscaledTime+.5f;
                fps.text = plugin.FramesPerSecond.ToString("0")+" FPS";
            }
            string text = plugin.Settings.VrNotifications.Value && Time.unscaledTime < messageUntil ?
                message : "Touch or point + trigger";
            if (feedback.text != text) feedback.text = text;

            Transform right = plugin.Game.RightHand;
            int touched = -1, aimed = -1, mouse = -1;
            Vector3 hit = Vector3.zero, mouseHit = Vector3.zero;
            bool ray = false, mouseRay = false, clicked = false;
            if (right != null)
            {
                Vector3 tip = root.transform.InverseTransformPoint(right.TransformPoint(new Vector3(0f,-.018f,.047f)));
                int candidate = Hit(tip.x/Unit,-tip.y/Unit);
                if (tip.z >= -.025f && tip.z <= .006f) touched = candidate;
                if (candidate < 0 || tip.z < -.045f || tip.z > .02f) touchReady = true;
                ray = Intersect(new Ray(right.position,-right.up),1.2f,out aimed,out hit);
                if (!ray || aimed < 0)
                {
                    int alternate; Vector3 alternateHit;
                    if (Intersect(new Ray(right.position,right.forward),1.2f,out alternate,out alternateHit) &&
                        (!ray || alternate >= 0))
                    { ray = true; aimed = alternate; hit = alternateHit; }
                }
            }
            else touchReady = false;
            if (preview || right == null)
            {
                try
                {
                    mouseRay = Intersect(camera.ScreenPointToRay(Input.mousePosition),4f,out mouse,out mouseHit);
                    clicked = mouse >= 0 && Input.GetMouseButtonDown(0);
                }
                catch (Exception) { }
            }
            int selection = clicked ? mouse : touched >= 0 ? touched : aimed >= 0 ? aimed : mouse;
            SetHover(selection);
            if (touched >= 0 && right != null)
            {
                Vector2 center = slots[touched].Area.center;
                ShowPointer(right.position,root.transform.TransformPoint(new Vector3(center.x*Unit,-center.y*Unit,0f)),false);
            }
            else if (ray && right != null) ShowPointer(right.position,hit,plugin.Settings.VrPointerRay.Value);
            else if (mouseRay) ShowPointer(mouseHit,mouseHit,false);
            else HidePointer();

            bool touch = touched >= 0 && touchReady;
            bool press = aimed >= 0 && trigger && !oldTrigger;
            oldTrigger = trigger;
            if (!(touch || press || clicked) || Time.unscaledTime < nextPress) return;
            int index = clicked ? mouse : touch ? touched : aimed;
            if (index < 0 || index >= slots.Count || !slots[index].Available) return;
            touchReady = false;
            nextPress = Time.unscaledTime+.22f;
            if (controller.isValid)
                try { controller.SendHapticImpulse(0u,.22f,.045f); } catch (Exception) { }
            slots[index].Action?.Invoke();
        }

        #endregion

        #region World-space UI construction

        private void Create()
        {
            font = RuntimeFont();
            if (font == null) throw new InvalidOperationException("No runtime UI font found.");
            root = new GameObject("Volas Wrist Panel");
            UnityEngine.Object.DontDestroyOnLoad(root);
            var obj = new GameObject("World UI",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));
            obj.transform.SetParent(root.transform,false);
            panel = obj.GetComponent<RectTransform>();
            panel.sizeDelta = new Vector2(Width,Height);
            panel.localScale = Vector3.one*Unit;
            var canvas = obj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 150;
            obj.GetComponent<CanvasScaler>().dynamicPixelsPerUnit = 2f;
            ImageAt(panel,0,0,Width,Height,Black);
            ImageAt(panel,12,12,456,64,Surface);
            TextAt(panel,"VOLAS",24,16,180,52,44,White,TextAnchor.MiddleLeft);
            fps = TextAt(panel,"",208,18,142,48,27,Muted,TextAnchor.MiddleRight);
            Button(364,20,92,48,"Close",()=>plugin.VrOpen=false,26);
            local = Button(16,90,84,54,"Local",()=>Open(Mode.Local),26);
            home = Button(106,90,84,54,"Cats",()=>Open(Mode.Home),26);
            saved = Button(196,90,84,54,"Saved",()=>Open(Mode.Saved),26);
            enabled = Button(286,90,84,54,"Active",()=>Open(Mode.Enabled),26);
            all = Button(376,90,88,54,"All",()=>Open(Mode.All),26);
            heading = TextAt(panel,"Categories",20,150,440,40,30,Muted,TextAnchor.MiddleLeft);
            for (int i=0;i<PerPage;i++)
            {
                float y=194+i*74;
                rows[i]=Button(16,y,368,66,"",null,36,TextAnchor.MiddleLeft);
                favorites[i]=Button(392,y,72,66,"",null,34);
            }
            previous=Button(16,500,112,58,"Prev",()=>Move(-1),32);
            pageLabel=TextAt(panel,"",132,500,136,58,30,White);
            reset=Button(272,500,76,58,"Stop",()=>{plugin.Features.DisableAll();Notify();},26);
            next=Button(352,500,112,58,"Next",()=>Move(1),32);
            ImageAt(panel,16,570,448,54,Surface);
            feedback=TextAt(panel,"",24,574,432,46,27,White);

            Shader shader=Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default");
            if (shader != null)
            {
                pointerMaterial=new Material(shader){color=White};
                dot=GameObject.CreatePrimitive(PrimitiveType.Sphere);
                dot.transform.SetParent(root.transform,false);
                dot.transform.localScale=Vector3.one*.007f;
                var collider=dot.GetComponent<Collider>();
                if (collider!=null) collider.enabled=false;
                dot.GetComponent<Renderer>().sharedMaterial=pointerMaterial;
                var ray=new GameObject("Selection ray");
                ray.transform.SetParent(root.transform,false);
                beam=ray.AddComponent<LineRenderer>();
                beam.useWorldSpace=true;
                beam.positionCount=2;
                beam.startWidth=beam.endWidth=.0015f;
                beam.sharedMaterial=pointerMaterial;
            }
            HidePointer();
        }

        private static Font RuntimeFont()
        {
            Font f=null;
            try { f=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch (Exception) { }
            if (f==null) { try { f=Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch (Exception) { } }
            return f ?? Font.CreateDynamicFontFromOSFont("Arial",36);
        }

        private RectTransform RectAt(Transform parent,float x,float y,float w,float h)
        {
            var r=new GameObject("Wrist element",typeof(RectTransform)).GetComponent<RectTransform>();
            r.SetParent(parent,false);
            r.anchorMin=r.anchorMax=new Vector2(0f,1f);
            r.pivot=new Vector2(0f,1f);
            r.anchoredPosition=new Vector2(x,-y);
            r.sizeDelta=new Vector2(w,h);
            return r;
        }
        private Image ImageAt(Transform parent,float x,float y,float w,float h,Color color)
        {
            var image=RectAt(parent,x,y,w,h).gameObject.AddComponent<Image>();
            image.color=color; image.raycastTarget=false;
            return image;
        }
        private Text TextAt(Transform parent,string text,float x,float y,float w,float h,int size,Color color,
            TextAnchor align=TextAnchor.MiddleCenter)
        {
            var t=RectAt(parent,x,y,w,h).gameObject.AddComponent<Text>();
            t.font=font; t.text=text; t.fontSize=size; t.fontStyle=FontStyle.Bold;
            t.color=color; t.alignment=align; t.raycastTarget=false; t.supportRichText=false;
            t.resizeTextForBestFit=true; t.resizeTextMinSize=22; t.resizeTextMaxSize=size;
            t.horizontalOverflow=HorizontalWrapMode.Wrap; t.verticalOverflow=VerticalWrapMode.Truncate;
            labels.Add(t); fontSizes.Add(size);
            return t;
        }
        private Slot Button(float x,float y,float w,float h,string caption,Action action,int size,TextAnchor align=TextAnchor.MiddleCenter)
        {
            var image=ImageAt(panel,x,y,w,h,Surface);
            float inset=align==TextAnchor.MiddleLeft?14f:4f;
            var t=TextAt(image.rectTransform,caption,inset,2,w-inset-4,h-4,size,White,align);
            var slot=new Slot {Area=new Rect(x-Width*.5f,y-Height*.5f,w,h),Image=image,Text=t,Action=action,Available=action!=null};
            slots.Add(slot); return slot;
        }
        private void Set(Slot s,string label,Action action,bool selected,bool show=true)
        {
            s.Action=action; s.Available=show && action!=null; s.Selected=selected;
            s.Image.gameObject.SetActive(show);
            if(s.Text.text!=label) s.Text.text=label;
            Paint(s,false);
        }
        private static void Paint(Slot s,bool hover)
        {
            s.Image.color=s.Selected?(hover ? new Color32(210,210,218,255) : White):hover?Gray:Surface;
            s.Text.color=s.Selected?Ink:s.Available?White:Muted;
        }
        #endregion

        #region Category & page state

        private void Refresh()
        {
            dirty=false; revision=plugin.Features.Revision; hovered=-1;
            Set(local,"Local",()=>Open(Mode.Local),mode==Mode.Local);
            Set(home,"Cats",()=>Open(Mode.Home),mode==Mode.Home||mode==Mode.Category);
            Set(saved,"Saved",()=>Open(Mode.Saved),mode==Mode.Saved);
            Set(enabled,"Active",()=>Open(Mode.Enabled),mode==Mode.Enabled);
            Set(all,"All",()=>Open(Mode.All),mode==Mode.All);
            List<string> categories=null; List<Feature> features=null;
            if(mode==Mode.Home)
            {
                categories=plugin.Features.Categories.FindAll(c=>c!="All"&&c!="Favorites");
                heading.text="Categories";
            }
            else
            {
                if(mode==Mode.Local) {features=plugin.Features.All.FindAll(f=>(f.Category=="Local"||f.Category=="Telemetry")&&!f.NeedsModdedRoom);heading.text="Local display tools";}
                else if(mode==Mode.Saved) {features=plugin.Features.InCategory("Favorites");heading.text="Favorites";}
                else if(mode==Mode.Enabled) {features=plugin.Features.All.FindAll(f=>f.Enabled);heading.text="Active features";}
                else if(mode==Mode.All) {features=plugin.Features.All;heading.text="All features";}
                else {features=plugin.Features.InCategory(category);heading.text=category;}
            }
            int count=categories!=null?categories.Count:features.Count;
            pageCount=Mathf.Max(1,(count+PerPage-1)/PerPage);
            page=Mathf.Clamp(page,0,pageCount-1);
            pageLabel.text=(page+1)+" / "+pageCount;
            Set(previous,"Prev",page>0?(Action)(()=>Move(-1)):null,false);
            Set(next,"Next",page+1<pageCount?(Action)(()=>Move(1)):null,false);
            for(int i=0;i<PerPage;i++)
            {
                int index=page*PerPage+i;
                if(index>=count) {Set(rows[i],"",null,false,false);Set(favorites[i],"",null,false,false);}
                else if(categories!=null)
                {
                    string name=categories[index];
                    Set(rows[i],name,()=>{category=name;Open(Mode.Category);},false);
                    Set(favorites[i],">",()=>{category=name;Open(Mode.Category);},false);
                }
                else
                {
                    Feature f=features[index];bool favorite=plugin.Features.IsFavorite(f);
                    bool locked=f.NeedsModdedRoom&&!roomAllowed&&!f.Enabled;
                    Set(rows[i],locked ? "[LOCK] "+f.Label : f.Label,()=>{plugin.Features.Activate(f);Notify();},f.Enabled);
                    if(locked) rows[i].Text.color=Muted;
                    Set(favorites[i],favorite?"*":"+",()=>{plugin.Features.ToggleFavorite(f);Notify();},favorite);
                }
            }
            if(count==0) Set(rows[0],"No entries",null,false);
        }
        private void Notify()
        {
            message=plugin.Features.LastMessage;messageUntil=Time.unscaledTime+3f;dirty=true;
        }
        private void Open(Mode m){mode=m;page=0;dirty=true;}
        private void Move(int direction){page=Mathf.Clamp(page+direction,0,pageCount-1);dirty=true;}
        private void UpdateFont()
        {
            float scale=plugin.Settings.VrTextScale.Value;
            if(Mathf.Abs(scale-lastFontScale)<.001f)return;
            lastFontScale=scale;
            for(int i=0;i<labels.Count;i++)
            {
                int size=Mathf.RoundToInt(fontSizes[i]*scale);
                labels[i].fontSize=size;labels[i].resizeTextMaxSize=size;
                labels[i].resizeTextMinSize=Mathf.Min(size,Mathf.RoundToInt(22f*scale));
            }
        }
        #endregion

        #region Pointer & controller input

        private int Hit(float x,float y)
        {
            for(int i=slots.Count-1;i>=0;i--)
                if(slots[i].Available&&slots[i].Area.Contains(new Vector2(x,y)))return i;
            return -1;
        }
        private void SetHover(int index)
        {
            if(hovered==index)return;
            if(hovered>=0)Paint(slots[hovered],false);
            hovered=index;if(index>=0)Paint(slots[index],true);
        }
        private bool Intersect(Ray ray,float max,out int index,out Vector3 hit)
        {
            index=-1;hit=Vector3.zero;
            Plane plane=new Plane(-root.transform.forward,root.transform.position);
            float d;if(!plane.Raycast(ray,out d)||d<0f||d>max)return false;
            hit=ray.GetPoint(d);Vector3 local=root.transform.InverseTransformPoint(hit);
            float x=local.x/Unit,y=-local.y/Unit;
            if(Mathf.Abs(x)>Width*.5f||Mathf.Abs(y)>Height*.5f)return false;
            index=Hit(x,y);return true;
        }
        private void ShowPointer(Vector3 start,Vector3 end,bool line)
        {
            if(dot==null||beam==null)return;
            dot.SetActive(true);dot.transform.position=end-root.transform.forward*.004f;
            beam.gameObject.SetActive(line);
            if(line){beam.SetPosition(0,start);beam.SetPosition(1,end);}
        }
        private void HidePointer(){if(dot!=null)dot.SetActive(false);if(beam!=null)beam.gameObject.SetActive(false);}
        private InputDevice RightDevice()
        {
            InputDevice device=InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            if(device.isValid)return device;
            devices.Clear();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Right|InputDeviceCharacteristics.Controller,devices);
            foreach(var d in devices)if(d.isValid)return d;
            return device;
        }
        private static bool ReadTrigger(InputDevice d)
        {
            if(!d.isValid)return false;
            bool pressed;if(d.TryGetFeatureValue(CommonUsages.triggerButton,out pressed)&&pressed)return true;
            float v;return d.TryGetFeatureValue(CommonUsages.trigger,out v)&&v>=.65f;
        }
        #endregion

        #region Resource cleanup

        internal void Dispose()
        {
            if(root!=null)UnityEngine.Object.Destroy(root);
            if(pointerMaterial!=null)UnityEngine.Object.Destroy(pointerMaterial);
            root=null;slots.Clear();labels.Clear();fontSizes.Clear();
        }

        #endregion
    }
}
