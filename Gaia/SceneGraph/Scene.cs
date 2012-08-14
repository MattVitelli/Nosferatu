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

        public Camera MainPlayer;

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
            Actors.Remove(entity);
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
            Player player = new Player();
            player.SetEnabled(true);
            player.SetControllable(true);
            Entities.Add("Player", player);
        }

        void InitializePhysics()
        {
            world = new PhysicsSystem();
            
            //world.CollisionSystem = new CollisionSystemGrid(64, 64, 64, 5, 5, 5);
            //physicSystem.CollisionSystem = new CollisionSystemBrute();
            world.CollisionSystem = new CollisionSystemSAP();

            world.EnableFreezing = true;
            world.SolverType = PhysicsSystem.Solver.Normal;
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

        void CreateLandmarks()
        {
            plant = new PowerPlant(this);
            gasStation = new GasStation(this);

            Model hangar = new Model("Hangar");
            (MainTerrain as TerrainVoxel).GetLandmarkTransform(MapLandmark.Hangar, hangar.Transformation, hangar.GetMesh().GetBounds());
            AddEntity("Hangar", hangar);

            Model plane = new Model("Plane");
            (MainTerrain as TerrainVoxel).GetLandmarkTransform(MapLandmark.Hangar, plane.Transformation, plane.GetMesh().GetBounds());
            AddEntity("Plane", plane);

            Model shack = new Model("Shack");
            (MainTerrain as TerrainVoxel).GetLandmarkTransform(MapLandmark.Docks, shack.Transformation, shack.GetMesh().GetBounds());
            AddEntity("Shack", shack);

            InteractObject key = new InteractObject(null, "Key");
            key.SetInteractNode(new PickupNode(key, PickupName.Key, "Power Plant Key")); 
            key.Transformation.SetPosition(shack.Transformation.GetPosition());
            key.Transformation.SetRotation(shack.Transformation.GetRotation());
            AddEntity("Key", key);

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

            Model crypt = new Model("TombStone");
            (MainTerrain as TerrainVoxel).GetLandmarkTransform(MapLandmark.Tomb, crypt.Transformation, crypt.GetMesh().GetBounds());
            AddEntity("Crypt", crypt);

            InteractObject amulet = new InteractObject(null, "Amulet");
            amulet.SetInteractNode(new PickupNode(amulet, PickupName.Amulet, "Amulet"));
            amulet.Transformation.SetPosition(crypt.Transformation.GetPosition());
            amulet.Transformation.SetRotation(crypt.Transformation.GetRotation());
            AddEntity("Amulet", amulet);


            Model tent = new Model("Tent");
            (MainTerrain as TerrainVoxel).GetLandmarkTransform(MapLandmark.Camp, tent.Transformation, tent.GetMesh().GetBounds());
            AddEntity("Tent", tent);

            Model campfire = new Model("Campfire");
            campfire.Transformation = tent.Transformation;
            AddEntity("CampFire", campfire);

            CampTrigger campTrigger = new CampTrigger();
            campTrigger.Transformation.SetPosition(tent.Transformation.GetPosition());
            campTrigger.Transformation.SetScale(Vector3.One * 15);
            AddEntity("CampTrigger", campTrigger);

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

            MainPlayer = new Camera();
            
            Entities.Add("MainCamera", MainPlayer);
            
            Entities.Add("Terrain", MainTerrain);
            Entities.Add("Light", MainLight);
            Entities.Add("AmbientLight", new Light(LightType.Ambient, new Vector3(0.15f, 0.35f, 0.55f), Vector3.Zero, false));
            

            //Entities.Add("Plane", new Model("Plane"));
            /*
            Entities.Add("TestTree", new Model("Palm02"));
            Entities["TestTree"].Transformation.SetPosition(Vector3.Forward * 10.0f);
            Entities.Add("TestTree2", new Model("JungleOverhang"));
            Entities["TestTree2"].Transformation.SetPosition(Vector3.Forward * 10.0f + Vector3.Right * 7.6f);
            */

            //AddEntity("Grass", new GrassPlacement());
            
            AddEntity("Forest", new ForestManager());
            CreateLandmarks();
            
            //CreateForest();

            //Entities.Add("Grass", new ShapePlacement());


            /*
            Model testGeom = new Model("test_level");
            testGeom.Transformation.SetPosition(Vector3.Up*20.0f);
            Entities.Add("scene_geom1", testGeom);

            Model testGeom2 = new Model("cgWarehouse002story");
            testGeom2.Transformation.SetPosition(Vector3.Up * 21.0f);
            Entities.Add("scene_geom2", testGeom2);
            */

            /*
            AnimatedModel model = new AnimatedModel("Allosaurus");
            
            model.Transformation.SetPosition(Vector3.Forward*10+Vector3.Up*68);
            model.Model.GetAnimationLayer().SetActiveAnimation("AllosaurusIdle", true);//.SetAnimationLayer("AllosaurusIdle", 1.0f);
            model.Model.SetCustomMatrix(Matrix.CreateScale(0.09f)*Matrix.CreateRotationX(-MathHelper.PiOver2));
            //model.UpdateAnimation();
            Entities.Add("TestCharacter", model);
            
            AnimatedModel model2 = new AnimatedModel("AlphaRaptor");

            model2.Transformation.SetPosition(Vector3.Forward * -5 + Vector3.Up * 62);
            model2.Model.GetAnimationLayer().SetActiveAnimation("AlphaRaptorIdle", true);//.SetAnimationLayer("AlphaRaptorIdle", 1.0f);
            model2.Model.SetCustomMatrix(Matrix.CreateScale(0.12f) * Matrix.CreateRotationX(-MathHelper.PiOver2));
            //model.UpdateAnimation();
            Entities.Add("TestCharacter2", model2);
            
            InteractObject weaponCrate = new InteractObject(new ChestNode("Weapon Box"), "WeaponBox");
            weaponCrate.Transformation.SetPosition(Vector3.Up * 30.0f);
            Entities.Add("weaponCrate", weaponCrate);
            */


            CreateTeams();          
            
            //Entities.Add("Light2", new Light(LightType.Directional, new Vector3(0.2797f, 0.344f, 0.43f), Vector3.Up, false));

            Initialize();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
        }

        public void Update()
        {
            DetermineSceneDimensions();

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
                }
            }
        }
    }
}
