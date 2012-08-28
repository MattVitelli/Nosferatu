using System;
using System.Collections.Generic;
using System.Xml;
using Microsoft.Xna.Framework;

using JigLibX.Physics;
using JigLibX.Collision;
using JigLibX.Geometry;

using Gaia.Core;
using Gaia.Rendering.RenderViews;
using Gaia.SceneGraph.GameEntities;
using Gaia.Resources;
using Gaia.Game;
using Gaia.Rendering;
using Gaia.Voxels;

namespace Gaia.SceneGraph
{
    public class Scene
    {
        public List<Actor> Actors = new List<Actor>();
        public SortedList<string, Entity> Entities = new SortedList<string, Entity>();
        List<RenderView>[] RenderViews = new List<RenderView>[(int)RenderViewType.Count];

        public Light MainLight; //Our sunlight
        
        public RenderView MainCamera;

        public Player MainPlayer;

        public AIDirector MainDirector;

        public Terrain MainTerrain;

        public PowerPlant plant;

        public GasStation gasStation;

        BoundingBox sceneDimensions;

        PhysicsSystem world;
       
        public Scene()
        {
            InitializeRenderViews();
            InitializeScene();
        }

        public Vector3 GetMainLightDirection()
        {
            if (MainLight != null)
                return MainLight.Transformation.GetPosition();

            return Vector3.Up;
        }

        public Vector3 GetMainLightDirectionSky()
        {
            if (MainLight != null)
                return (MainLight as Sunlight).GetSkyDirection();

            return Vector3.Up;
        }

        public PhysicsSystem GetPhysicsEngine()
        {
            return world;
        }

        public BoundingBox GetSceneDimensions()
        {
            return sceneDimensions;
        }

        string GetNextAvailableName(string name)
        {
            int count = 0;
            string newName = name;
            while (Entities.ContainsKey(newName))
            {
                count++;
                newName = name + count;
            }
            return newName;
        }

        public void AddEntity(string name, Entity entity)
        {
            string newName = GetNextAvailableName(name);
            Entities.Add(newName, entity);
            entity.OnAdd(this);
        }

        public string RenameEntity(string oldName, string newName)
        {
            if (Entities.ContainsKey(oldName))
            {
                Entity ent = Entities[oldName];
                Entities.Remove(oldName);
                newName = GetNextAvailableName(newName);
                Entities.Add(newName, ent);
                return newName;
            }

            return oldName;
        }

        public Entity FindEntity(string name)
        {
            if (Entities.ContainsKey(name))
                return Entities[name];

            return null;
        }

        public void RemoveEntity(string name)
        {
            Entities[name].OnDestroy();
            Entities.Remove(name);
        }

        public void RemoveEntity(Entity entity)
        {
            int index = Entities.IndexOfValue(entity);
            if (index >= 0)
            {
                Entities.Values[index].OnDestroy();
                Entities.RemoveAt(index);
            }
        }

        public void AddActor(Actor entity)
        {
            Actors.Add(entity);
        }

        public void RemoveActor(Actor entity)
        {
            int index = Actors.IndexOf(entity);
            if (index >= 0)
                Actors.RemoveAt(index);
        }

        public void AddRenderView(RenderView view)
        {
            RenderViews[(int)view.GetRenderType()].Add(view);
        }

        public void RemoveRenderView(RenderView view)
        {
            int index = RenderViews[(int)view.GetRenderType()].IndexOf(view);
            if (index >= 0)
                RenderViews[(int)view.GetRenderType()].RemoveAt(index);
        }

        void Initialize()
        {
            for (int i = 0; i < Entities.Count; i++)
            {
                Entities.Values[i].OnAdd(this);
            }
        }

        public void Destroy()
        {
            for (int i = 0; i < Entities.Count; i++)
            {
                Entities.Values[i].OnDestroy();
            }
        }

        void DetermineSceneDimensions()
        {
            sceneDimensions.Max = Vector3.One * float.NegativeInfinity;
            sceneDimensions.Min = Vector3.One * float.PositiveInfinity;
            for (int i = 0; i < Entities.Count; i++)
            {
                BoundingBox bounds = Entities.Values[i].Transformation.GetBounds();
                sceneDimensions.Min = Vector3.Min(sceneDimensions.Min, bounds.Min);
                sceneDimensions.Max = Vector3.Max(sceneDimensions.Max, bounds.Max);
            }
        }

        void CreateTeams()
        {
            MainPlayer = new Player();
            MainPlayer.SetEnabled(true);
            MainPlayer.SetControllable(true);
            MainPlayer.SetSpawnPosition(FindEntity("Tent").Transformation.GetPosition());
            //MainPlayer.SetSpawnPosition(FindEntity("Tent").Transformation.GetPosition());
            Entities.Add("Player", MainPlayer);
            MainDirector = new AIDirector();
            Entities.Add("AIDirector", MainDirector);
            Entities.Add("SeaMonster", new SeaMonster());
        }

        void InitializePhysics()
        {
            world = new PhysicsSystem();
            
            //world.CollisionSystem = new CollisionSystemGrid(64, 64, 64, 5, 5, 5);
            //physicSystem.CollisionSystem = new CollisionSystemBrute();
            world.CollisionSystem = new CollisionSystemSAP();

            world.EnableFreezing = true;
            world.SolverType = PhysicsSystem.Solver.Combined;
            world.CollisionSystem.UseSweepTests = true;
            world.Gravity = new Vector3(0, -10, 0);//PhysicsHelper.GravityEarth, 0);
            
            world.NumCollisionIterations = 30;
            world.NumContactIterations = 30;
            world.NumPenetrationRelaxtionTimesteps = 30;
        }

        public void ResetScene()
        {
            for (int i = 0; i < Entities.Count; i++)
                Entities.Values[i].OnDestroy();
            Entities.Clear();
            for (int i = 0; i < RenderViews.Length; i++)
                RenderViews[i].Clear();
            //RenderViews.Clear();
            InitializePhysics();
        }

        public void SaveScene(XmlWriter writer)
        {
            writer.WriteStartElement("Scene");
            for (int i = 0; i < Entities.Count; i++)
            {
                string key = Entities.Keys[i];
                writer.WriteStartElement(Entities[key].GetType().FullName);
                writer.WriteStartAttribute("name");
                writer.WriteValue(key);
                writer.WriteEndAttribute();
                Entities.Values[i].OnSave(writer);
                writer.WriteEndElement();
                writer.WriteWhitespace("\n");
            }
            writer.WriteEndElement();
        }

        public void LoadScene(string filename)
        {
            ResetScene();
            XmlDocument doc = new XmlDocument();
            doc.Load(filename);
            XmlNodeList roots = doc.GetElementsByTagName("Scene");
            foreach (XmlNode root in roots)
            {
                foreach(XmlNode node in root.ChildNodes)
                {
                    Type entityType = Type.GetType(node.Name);
                    if (entityType.IsSubclassOf(typeof(Entity)))
                    {
                        Entity entity = (Entity)entityType.GetConstructors()[0].Invoke(null);
                        entity.OnLoad(node);
                        Entities.Add(node.Attributes["name"].Value, entity); 
                    }
                }
            }

            Initialize();
        }

        void CreateForest()
        {
            /*
            List<TriangleGraph> availableTriangles;
            BoundingBox region = MainTerrain.Transformation.GetBounds();
            if (MainTerrain.GetTrianglesInRegion(RandomHelper.RandomGen, out availableTriangles, region))
            {
                for (int i = 0; i < 300; i++)
                {
                    Model tree = new Model("Palm02");
                    NormalTransform transform = new NormalTransform();
                    tree.Transformation = transform;
                    int randomIndex = RandomHelper.RandomGen.Next(i % availableTriangles.Count, availableTriangles.Count);
                    TriangleGraph triangle = availableTriangles[randomIndex];
                    Vector3 position = triangle.GeneratePointInTriangle(RandomHelper.RandomGen);
                    Vector3 normal = triangle.Normal;
                    transform.ConformToNormal(normal);
                    transform.SetPosition(position);
                    AddEntity("Tree", tree);
                }
            }
            availableTriangles.Clear();
            GC.Collect();
            */

            for (int i = 0; i < 6000; i++)
            {
                Model tree = new Model("Cecropia");
                //NormalTransform transform = new NormalTransform();
                //tree.Transformation = transform;
                Vector3 pos;
                Vector3 normal;
                MainTerrain.GenerateRandomTransform(RandomHelper.RandomGen, out pos, out normal);
                //transform.ConformToNormal(normal);
                tree.Transformation.SetPosition(pos);
                AddEntity("T", tree);
            }
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
        }

        void CreateStartZone()
        {
            Model tent = new Model("MilitaryTent");
            (MainTerrain as TerrainVoxel).GetLandmarkTransform(MapLandmark.Camp, tent.Transformation, tent.GetMesh().GetBounds());
            AddEntity("Tent", tent);

            AnimationNode[] nodes = tent.GetMesh().GetNodes();
            Matrix worldMatrix = tent.Transformation.GetTransform();
            //Vector3 meshCenter = 0.5f * (gasStation.GetMesh().GetBounds().Max + gasStation.GetMesh().GetBounds().Min);
            for (int i = 0; i < nodes.Length; i++)
            {
                Vector3 nodePos = Vector3.Transform(nodes[i].Translation, worldMatrix);
                if (nodes[i].Name == "Bed")
                {
                    InteractObject bed = new InteractObject(new BedNode(this), "Bed");
                    bed.Transformation.SetPosition(nodePos);
                    bed.Transformation.SetRotation(tent.Transformation.GetRotation());
                    AddEntity("Bed", bed);
                }
                if(nodes[i].Name == "Ammo")
                {
                    InteractObject weaponBox = new InteractObject(new WeaponBoxNode(this), "AmmoBox");
                    weaponBox.Transformation.SetPosition(nodePos);
                    weaponBox.Transformation.SetRotation(tent.Transformation.GetRotation());
                    AddEntity("WeaponBox", weaponBox);
                }
            }

            SafeTrigger campTrigger = new SafeTrigger();
            campTrigger.Transformation.SetPosition(tent.Transformation.GetPosition());
            campTrigger.Transformation.SetScale(Vector3.One * 15);
            AddEntity("CampTrigger", campTrigger);

            /*
            Model campfire = new Model("Campfire");
            campfire.Transformation = tent.Transformation;
            AddEntity("CampFire", campfire);

            ParticleEmitter flameEmitter = new ParticleEmitter(ResourceManager.Inst.GetParticleEffect("Fire0"), 300);
            flameEmitter.Transformation.SetPosition(campfire.Transformation.GetPosition() + Vector3.Up * 1.5f);
            AddEntity("FireEffect", flameEmitter);

            ParticleEmitter smokeEmitter = new ParticleEmitter(ResourceManager.Inst.GetParticleEffect("Smoke4"), 300);
            smokeEmitter.Transformation.SetPosition(campfire.Transformation.GetPosition() + Vector3.Up * 3.0f);
            AddEntity("SmokeEffect", smokeEmitter);
            */
        }

        public bool TestBoundsVisibility(BoundingBox bounds)
        {
            for (int i = 0; i < RenderViews.Length; i++)
            {
                for (int j = 0; j < RenderViews[i].Count; j++)
                {
                    if (RenderViews[i][j].GetFrustum().Contains(bounds) != ContainmentType.Disjoint)
                        return true;
                }
            }

            return false;
        }

        void CreateHangar()
        {
            Model hangar = new Model("Hangar");
            (MainTerrain as TerrainVoxel).GetLandmarkTransform(MapLandmark.Hangar, hangar.Transformation, hangar.GetMesh().GetBounds());
            AddEntity("Hangar", hangar);

            SafeTrigger campTrigger = new SafeTrigger();
            campTrigger.Transformation.SetPosition(hangar.Transformation.GetPosition());
            campTrigger.Transformation.SetScale(Vector3.One * 30);
            AddEntity("SafeTrigger", campTrigger);

            AnimationNode[] nodes = hangar.GetMesh().GetNodes();
            Matrix worldMatrix = hangar.Transformation.GetTransform();
            Vector3 rot = hangar.Transformation.GetRotation();
            Vector3 camNodePos = Vector3.Zero;
            for (int i = 0; i < nodes.Length; i++)
            {
                Vector3 nodePos = Vector3.Transform(nodes[i].Translation, worldMatrix);
                switch (nodes[i].Name)
                {
                    case "Plane":
                        InteractObject plane = new InteractObject(new AirplaneNode(this), "Plane");
                        plane.Transformation.SetPosition(nodePos);
                        plane.Transformation.SetRotation(rot);
                        AddEntity("Plane", plane);
                        break;
                    case "Door":
                        InteractObject door = new InteractObject(null, "HangarDoor", true);
                        Vector3 dispVec = Vector3.TransformNormal(Vector3.Right, worldMatrix)*-4f;
                        door.Transformation.SetPosition(nodePos);
                        door.Transformation.SetRotation(rot);
                        door.SetInteractNode(new HangarDoorNode(door, Vector3.Zero, dispVec));
                        AddEntity("HangarDoor", door);
                        break;
                    case "Garage":
                        InteractObject garage = new InteractObject(null, "HangarGarage", true);
                        garage.Transformation.SetPosition(nodePos);
                        garage.Transformation.SetRotation(rot);
                        garage.SetInteractNode(new HangarDoorNode(garage, new Vector3(0, 0, -MathHelper.PiOver2), Vector3.Zero));
                        AddEntity("HangarGarage", garage);
                        break;
                    case "DinoSpawn":
                        AnimatedModel trex = new AnimatedModel("TRex");
                        trex.SetCulling(false);
                        trex.Transformation.SetPosition(nodePos);
                        trex.Model.GetAnimationLayer().SetActiveAnimation("TRexIdle", true);//.SetAnimationLayer("AlphaRaptorIdle", 1.0f);
                        trex.Model.SetCustomMatrix(Matrix.CreateScale(0.1f) * Matrix.CreateRotationX(-MathHelper.PiOver2) * Matrix.CreateRotationY(-MathHelper.PiOver2));
                        //model.UpdateAnimation();
                        AddEntity("TRex", trex);
                        trex.SetVisible(false);
                        break;
                    case "CameraNode":
                        camNodePos = nodePos;
                        break;
                }

                InteractObject tempPlane = (InteractObject)FindEntity("Plane");
                AirplaneNode planeNode = (AirplaneNode)tempPlane.GetInteractNode();
                planeNode.SetEndCameraPosition(camNodePos, Vector3.TransformNormal(Vector3.Forward, worldMatrix));
                //InteractObject gasTank = new InteractObject(null, "GasTank");
                //gasTank.SetInteractNode(new GasTankNode(gasTank));
            }


            
        }

        void CreateLandmarks()
        {
            plant = new PowerPlant(this);
            gasStation = new GasStation(this);

            CreateHangar();

            Model shack = new Model("Shack");
            (MainTerrain as TerrainVoxel).GetLandmarkTransform(MapLandmark.Docks, shack.Transformation, shack.GetMesh().GetBounds());
            AddEntity("Shack", shack);

            InteractObject key = new InteractObject(null, "Key");
            key.SetInteractNode(new PickupNode(key, PickupName.Key, "Key")); 
            key.Transformation.SetPosition(shack.Transformation.GetPosition());
            key.Transformation.SetRotation(shack.Transformation.GetRotation());
            AddEntity("Key", key);

            SafeTrigger campTrigger = new SafeTrigger();
            campTrigger.Transformation.SetPosition(shack.Transformation.GetPosition());
            campTrigger.Transformation.SetScale(Vector3.One * 30);
            AddEntity("SafeTrigger", campTrigger);

            Model arena = new Model("Arena");
            (MainTerrain as TerrainVoxel).GetLandmarkTransform(MapLandmark.Meadow, arena.Transformation, arena.GetMesh().GetBounds());
            AddEntity("Arena", arena);

            Model windmill = new Model("Windmill");
            (MainTerrain as TerrainVoxel).GetLandmarkTransform(MapLandmark.Windmill, windmill.Transformation, windmill.GetMesh().GetBounds());
            AddEntity("Windmill", windmill);

            Model well = new Model("Well");
            (MainTerrain as TerrainVoxel).GetLandmarkTransform(MapLandmark.Well, well.Transformation, well.GetMesh().GetBounds());
            AddEntity("Well", well);

            Model tower = new Model("RadioTower");
            (MainTerrain as TerrainVoxel).GetLandmarkTransform(MapLandmark.RadioTower, tower.Transformation, tower.GetMesh().GetBounds());
            AddEntity("RadioTower", tower);

            CreateStartZone();

            BoundingBox sandBounds = MainTerrain.Transformation.GetBounds();
            sandBounds.Max.Y = 0;
            (MainTerrain as TerrainVoxel).SetUnavailableRegion(sandBounds);

            for (int i = 0; i < 50; i++)
            {
                Vector3 normalWeapon;
                Vector3 posWeapon;
                (MainTerrain as TerrainVoxel).GenerateRandomTransform(RandomHelper.RandomGen, out posWeapon, out normalWeapon);
                InteractObject weaponBox = new InteractObject(new WeaponBoxNode(this), "AmmoBox");
                NormalTransform transform = new NormalTransform();
                weaponBox.Transformation = transform;
                transform.ConformToNormal(normalWeapon);
                transform.SetPosition(posWeapon);
                AddEntity("WeaponBox", weaponBox);
            }

            sandBounds = MainTerrain.Transformation.GetBounds();
            sandBounds.Max.Y = 16;
            (MainTerrain as TerrainVoxel).SetUnavailableRegion(sandBounds);
        }

        void InitializeRenderViews()
        {
            for (int i = 0; i < RenderViews.Length; i++)
            {
                RenderViews[i] = new List<RenderView>();
            }
        }

        void InitializeScene()
        {
            ResetScene();

            Entities.Add("Sky", new Sky());
            MainLight = new Sunlight();
            MainTerrain = new TerrainVoxel();

            /*
            MainTerrain = new TerrainHeightmap("Textures/Island_HM.png", 0, 100.0f);
            float width = (MainTerrain as TerrainHeightmap).GetWidth();
            float height = (MainTerrain as TerrainHeightmap).MaximumHeight;
            MainTerrain.Transformation.SetScale(new Vector3(1, height / width, 1) * width);
            */

            Entities.Add("MainCamera", new Camera());
            
            Entities.Add("Terrain", MainTerrain);
            Entities.Add("Light", MainLight);
            Entities.Add("AmbientLight", new Light(LightType.Ambient, new Vector3(0.15f, 0.35f, 0.55f), Vector3.Zero, false));

            CreateLandmarks();
            
            AddEntity("Grass", new ClusterManager(new string[]{"Bush", "Fern", "Phila01", "ElephantEar", "BirdsNest", "PalmPlant", "TropicalPlant"}, 20, true));//120, true));
            
            /*
            ForestManager grassMgr = new ForestManager(new string[] { "Bush", "Fern", "Phila01", "ElephantEar", "BirdsNest", "PalmPlant", "TropicalPlant" }, 8192);
            grassMgr.alignToSurface = true;
            AddEntity("Grass", grassMgr);
            */
            AddEntity("Forest", new ForestManager(new string[] { "Cecropia", "Palm02", "QueensPalm01", "Tree01", "Tree02", "Palm01", "BeachPalm"}, 3000));
            /*
            Raptor raptor = new Raptor(ResourceManager.Inst.GetDinosaurDatablock("Allosaurus"));
            raptor.SetSpawnPosition(FindEntity("Tent").Transformation.GetPosition());
            AddEntity("Raptor", raptor);
            */            
            CreateTeams();
            
            //Entities.Add("Light2", new Light(LightType.Directional, new Vector3(0.2797f, 0.344f, 0.43f), Vector3.Up, false));

            Initialize();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
        }

        void UpdateSoundListener()
        {
            Vector3 pos = MainCamera.GetPosition();
            Vector3 fwd = MainCamera.GetWorldMatrix().Forward;
            SoundEngine.Device.SetListenerPosition(new IrrKlang.Vector3D(pos.X, pos.Y, pos.Z), new IrrKlang.Vector3D(fwd.X, fwd.Y, fwd.Z));
            SoundEngine.Device.SetRolloffFactor(0.15f);
        }

        public void Update()
        {
            DetermineSceneDimensions();
            UpdateSoundListener();
            
            for (int i = 0; i < Entities.Count; i++)
            {
                Entities.Values[i].OnUpdate();       
            }

            float timeStep = Time.GameTime.ElapsedTime;
            if (timeStep < 1.0f / 60.0f)
                world.Integrate(timeStep);
            else
                world.Integrate(1.0f / 60.0f);
        }

        public void Render()
        {
            for (int i = 0; i < RenderViews.Length; i++)
            {
                for (int j = 0; j < RenderViews[i].Count; j++)
                {
                    for (int k = 0; k < Entities.Count; k++)
                    {
                        Entities.Values[k].OnRender(RenderViews[i][j]);
                    }

                    RenderViews[i][j].Render();
                    GFX.Inst.CleanupPostFrame();
                }
            }
        }
    }
}
