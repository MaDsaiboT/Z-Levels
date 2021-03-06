﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ZLevels
{
    public class ZLevelsManager : GameComponent
    {
        public ZLevelsManager()
        {

        }

        public ZLevelsManager(Game game)
        {

        }

        public override void GameComponentOnGUI()
        {
            base.GameComponentOnGUI();
            this.CheckHotkeys();
        }

        //public override void GameComponentTick()
        //{
        //    base.GameComponentTick();
        //    if (Find.TickManager.TicksGame % 600 == 0)
        //    {
        //        foreach (var data in this.ZLevelsTracker)
        //        {
        //            foreach (var mapData in data.Value.ZLevels.Values)
        //            {
        //                foreach (var pawn in mapData.mapPawns.AllPawns)
        //                {
        //                    foreach (var mapData2 in data.Value.ZLevels.Values)
        //                    {
        //                        mapData2.dynamicDrawManager.RegisterDrawable(pawn);
        //                    }
        //                }
        //            }
        //        }
        //    }
        //}

        public void Select(object obj, bool playSound = true, bool forceDesignatorDeselect = true)
        {
            if (obj == null)
            {
                Log.Error("Cannot select null.", false);
                return;
            }
            Thing thing = obj as Thing;
            if (thing == null && !(obj is Zone))
            {
                Log.Error("Tried to select " + obj + " which is neither a Thing nor a Zone.", false);
                return;
            }
            if (thing != null && thing.Destroyed)
            {
                Log.Error("Cannot select destroyed thing.", false);
                return;
            }
            Pawn pawn = obj as Pawn;
            if (pawn != null && pawn.IsWorldPawn())
            {
                Log.Error("Cannot select world pawns.", false);
                return;
            }
            if (forceDesignatorDeselect)
            {
                Find.DesignatorManager.Deselect();
            }
            if (Find.Selector.SelectedZone != null && !(obj is Zone))
            {
                Find.Selector.ClearSelection();
            }
            if (obj is Zone && Find.Selector.SelectedZone == null)
            {
                Find.Selector.ClearSelection();
            }
            Map map = (thing != null) ? thing.Map : ((Zone)obj).Map;

            List<object> selected = Traverse.Create(Find.Selector).Field("selected").GetValue<List<object>>();

            for (int i = selected.Count - 1; i >= 0; i--)
            {
                Thing thing2 = selected[i] as Thing;
                if (((thing2 != null) ? thing2.Map : ((Zone)selected[i]).Map) != map)
                {
                    //Find.Selector.Deselect(selected[i]);
                }
            }
            if (selected.Count >= 200)
            {
                return;
            }
            if (!Find.Selector.IsSelected(obj))
            {
                selected.Add(obj);
                SelectionDrawer.Notify_Selected(obj);
            }
            Traverse.Create(Find.Selector).Field("selected").SetValue(selected);
        }
        public void CheckHotkeys()
        {
            bool keyDownEvent = ZLevelsDefOf.ZL_switchToUpperMap.KeyDownEvent;
            if (keyDownEvent)
            {
                Map mapToSwitch = this.GetUpperLevel(Find.CurrentMap.Tile, Find.CurrentMap);
                if (mapToSwitch != null)
                {
                    //var selectedObjects = Find.Selector.SelectedObjects.ListFullCopy();

                    var rememberedCamera = Current.Game.CurrentMap.rememberedCameraPos;
                    Current.Game.CurrentMap = mapToSwitch;
                    Find.CameraDriver.SetRootPosAndSize(rememberedCamera.rootPos, rememberedCamera.rootSize);

                    //foreach (var select in selectedObjects)
                    //{
                    //    this.Select(select);
                    //}
                }
                Event.current.Use();
            }
            bool keyDownEvent2 = ZLevelsDefOf.ZL_switchToLowerMap.KeyDownEvent;
            if (keyDownEvent2)
            {
                Map mapToSwitch = this.GetLowerLevel(Find.CurrentMap.Tile, Find.CurrentMap);
                if (mapToSwitch != null)
                {
                    //var selectedObjects = Find.Selector.SelectedObjects.ListFullCopy();

                    var rememberedCamera = Current.Game.CurrentMap.rememberedCameraPos;
                    Current.Game.CurrentMap = mapToSwitch;
                    Find.CameraDriver.SetRootPosAndSize(rememberedCamera.rootPos, rememberedCamera.rootSize);

                    //foreach (var select in selectedObjects)
                    //{
                    //    Log.Message("Select");
                    //    this.Select(select);
                    //}
                }
                Event.current.Use();
            }
        }

        public void SaveArea(Pawn pawn)
        {
            if (this.activeAreas == null) this.activeAreas = new Dictionary<Pawn, ActiveArea>();
            if (this.activeAreas.ContainsKey(pawn))
            {
                if (this.activeAreas[pawn].activeAreas == null)
                {
                    this.activeAreas[pawn].activeAreas = new Dictionary<Map, Area>()
                    {
                        {pawn.Map, pawn.playerSettings.AreaRestriction}
                    };
                }
                else
                {
                    this.activeAreas[pawn].activeAreas[pawn.Map] = pawn.playerSettings.AreaRestriction;
                }
            }
            else
            {
                this.activeAreas[pawn] = new ActiveArea();
                this.activeAreas[pawn].activeAreas = new Dictionary<Map, Area>()
                {
                    {pawn.Map, pawn.playerSettings.AreaRestriction}
                };
            }

            //foreach (var test in this.activeAreas)
            //{
            //    ZLogger.Message("Pawn: " + test.Key);
            //    foreach (var d in test.Value.activeAreas)
            //    {
            //        ZLogger.Message("ActiveAreas: " + d);
            //    }
            //}
        }

        public void LoadArea(Pawn pawn)
        {
            if (this.activeAreas.ContainsKey(pawn) &&
                this.activeAreas[pawn].activeAreas.ContainsKey(pawn.Map))
            {
                pawn.playerSettings.AreaRestriction = this.activeAreas[pawn].activeAreas[pawn.Map];
            }
            else
            {
                pawn.playerSettings.AreaRestriction = null;
            }
        }

        public Map GetUpperLevel(int tile, Map map)
        {
            //ZLogger.Message("Index to get: " + this.GetZIndexFor(map));
            if (this.ZLevelsTracker != null && this.ZLevelsTracker.ContainsKey(tile)
                && this.ZLevelsTracker[tile].ZLevels.ContainsKey(this.GetZIndexFor(map) + 1))
            {
                //foreach (var d in this.ZLevelsTracker[tile].ZLevels)
                //{
                //    ZLogger.Message("Data: " + d.Key + " - " + d.Value);
                //}
                //ZLogger.Message("Getting: " + this.ZLevelsTracker[tile].ZLevels[this.GetZIndexFor(map) + 1]);

                //ZLogger.Message("Z_Levels contains key, getting map:" + Z_Levels[Z_LevelIndex + 1]);
                return this.ZLevelsTracker[tile].ZLevels[this.GetZIndexFor(map) + 1];
            }
            return null;
        }
        public Map GetLowerLevel(int tile, Map map)
        {
            //ZLogger.Message("Current map index: " + Z_LevelIndex);
            //ZLogger.Message("Trying to get index:" + (Z_LevelIndex - 1));
            if (this.ZLevelsTracker != null && this.ZLevelsTracker.ContainsKey(tile)
                && this.ZLevelsTracker[tile].ZLevels.ContainsKey(this.GetZIndexFor(map) - 1))
            {
                //ZLogger.Message("Z_Levels contains key, getting map:" + Z_Levels[Z_LevelIndex + 1]);
                return this.ZLevelsTracker[tile].ZLevels[this.GetZIndexFor(map) - 1];
            }
            return null;
        }

        public List<Map> GetAllMaps(int tile)
        {
            List<Map> maps = new List<Map>();
            try
            {
                foreach (var map in this.ZLevelsTracker[tile].ZLevels.Values)
                {
                    maps.Add(map);
                }
                return maps;
            }
            catch
            {
                Log.Error("GetAllMaps returned null on " + tile);
                return null;
            }
        }

        public List<Map> GetAllMapsInClosestOrder(Map pawnMap)
        {
            List<Map> maps = new List<Map>();
            try
            {
                foreach (var map in this.ZLevelsTracker[pawnMap.Tile].ZLevels.Values.OrderBy(x =>
                (int)Mathf.Abs(this.GetZIndexFor(x) - this.GetZIndexFor(pawnMap))))
                {
                    maps.Add(map);
                }
                return maps;
            }
            catch
            {
                Log.Error("GetAllMaps returned null on " + pawnMap);
                return null;
            }
        }

        public int GetZIndexFor(Map map)
        {
            try
            {
                this.ZLevelsFixer(map.Tile);
                var comp = map.GetComponent<MapComponentZLevel>();
                return comp.Z_LevelIndex;
            }
            catch (Exception ex)
            {
                Log.Error("[Z-Levels] GetZIndexFor produced an error. " +
                    "That should not happen and will break things. " +
                    "Send a Hugslib log to the Z-Levels developers. Error message: " + ex, true);
                return -99999; 
            }
        }

        public Map GetMapByIndex(int tile, int index)
        {
            return this.ZLevelsTracker[tile].ZLevels[index];
        }

        public string GetMapInfo(Map map)
        {
            return "(" + map + " - Level " + GetZIndexFor(map) + ")";
        }

        public bool TryRegisterMap(Map map, int index)
        {
            if (this.ZLevelsTracker == null)
            {
                this.ZLevelsTracker = new Dictionary<int, ZLevelData>();
            }
            if (this.ZLevelsTracker.ContainsKey(map.Tile))
            {
                if (this.ZLevelsTracker[map.Tile].ZLevels == null)
                    this.ZLevelsTracker[map.Tile].ZLevels = new Dictionary<int, Map>();
                this.ZLevelsTracker[map.Tile].ZLevels[index] = map;
                ZLogger.Message("Registering " + this.GetMapInfo(map) + " for index: " + index);
                return true;
            }
            else
            {
                this.ZLevelsTracker[map.Tile] = new ZLevelData();
                this.ZLevelsTracker[map.Tile].ZLevels = new Dictionary<int, Map>();
                this.ZLevelsTracker[map.Tile].ZLevels[index] = map;
                ZLogger.Message("Registering " + this.GetMapInfo(map) + " for index: " + index);
                return true;
            }
            return false;
        }

        //public override void GameComponentTick()
        //{
        //    base.GameComponentTick();
        //    if (Find.TickManager.TicksGame % 200 == 0)
        //    {
        //        foreach (var t in this.ZLevelsTracker)
        //        {
        //            foreach (var d in this.ZLevelsTracker[t.Key].ZLevels)
        //            {
        //                ZLogger.Message(this.GetMapInfo(d.Value) + " - " + d.Value.weatherManager.curWeather
        //                    + " - " + d.Value.weatherManager.curWeatherAge + " - " + d.Value.weatherManager.lastWeather);
        //
        //            }
        //        }
        //        ZLogger.Message("========================");
        //    }
        //}
        public List<Job> HaulThingToDest(Pawn pawn, Thing thing, Map dest, ref IntVec3 lastStairsPosition, ref bool fail)
        {
            List<Job> tempJobs = new List<Job>();
            //ZLogger.Message(pawn + " - ???++++++++++++++++++++++++++++++++++++++++???");
            //ZLogger.Message(pawn + " - Hauling " + thing + "(" + thing.Map + ") from " + pawn.Map + " to " + dest);
            //ZLogger.Message(pawn + " - Current pawn map: " + pawn.Map);
            //ZLogger.Message(pawn + " - Thing map: " + thing.Map);
            //ZLogger.Message(pawn + " - Dest map: " + dest);
            if (this.GetZIndexFor(thing.Map) > this.GetZIndexFor(dest))
            {
                ZLogger.Message("2 - Build tree (HaulThingToDest): " + pawn + " - Going down");
                foreach (var map in this.ZLevelsTracker[pawn.Map.Tile].ZLevels.Values.OrderByDescending(x => this.GetZIndexFor(x)))
                {
                    if (this.GetZIndexFor(thing.Map) >= this.GetZIndexFor(map) &&
                        this.GetZIndexFor(map) >= this.GetZIndexFor(dest))
                    {
                        if (map != dest)
                        {
                            var stairs = map.listerThings.AllThings.Where(x => x is Building_StairsDown && x.Spawned).ToList();
                            if (stairs?.Count() > 0)
                            {
                                var selectedStairs = GenClosest.ClosestThing_Global(lastStairsPosition, stairs, 99999f);
                                if (selectedStairs != null)
                                {
                                    lastStairsPosition = selectedStairs.Position;
                                    Job gotoStairs = null;
                                    if (thing.Map == map)
                                    {
                                        ZLogger.Message("Build tree (HaulThingToDest): " + pawn + " - Hauling " + thing + " to "
                                            + selectedStairs + " in " + this.GetMapInfo(map));
                                        gotoStairs = JobMaker.MakeJob(ZLevelsDefOf.ZL_HaulThingToStairs,
                                            selectedStairs, thing);
                                    }
                                    else
                                    {
                                        ZLogger.Message("Build tree (HaulThingToDest): " + pawn + " - Finding and using " + selectedStairs
                                            + " in " + this.GetMapInfo(map));
                                        gotoStairs = JobMaker.MakeJob(ZLevelsDefOf.ZL_GoToStairs, selectedStairs);
                                    }
                                    tempJobs.Add(gotoStairs);
                                }
                                else
                                {
                                    fail = true;
                                }
                            }
                            else
                            {
                                fail = true;
                            }
                        }
                    }
                }
            }
            else if (this.GetZIndexFor(thing.Map) < this.GetZIndexFor(dest))
            {
                ZLogger.Message("2 - Build tree (HaulThingToDest): " + pawn + " - Going up");
                foreach (var map in this.ZLevelsTracker[pawn.Map.Tile].ZLevels.Values.OrderBy(x => this.GetZIndexFor(x)))
                {
                    if (this.GetZIndexFor(thing.Map) <= this.GetZIndexFor(map) &&
                        this.GetZIndexFor(map) <= this.GetZIndexFor(dest))
                    {
                        if (map != dest)
                        {
                            var stairs = map.listerThings.AllThings.Where(x => x is Building_StairsUp && x.Spawned).ToList();
                            if (stairs?.Count() > 0)
                            {
                                var selectedStairs = GenClosest.ClosestThing_Global(lastStairsPosition, stairs, 99999f);
                                if (selectedStairs != null)
                                {
                                    lastStairsPosition = selectedStairs.Position;
                                    Job gotoStairs = null;
                                    if (thing.Map == map)
                                    {
                                        ZLogger.Message("Build tree (HaulThingToDest): " + pawn + " - Hauling " + thing + " to "
                                            + selectedStairs + " in " + this.GetMapInfo(map));
                                        gotoStairs = JobMaker.MakeJob(ZLevelsDefOf.ZL_HaulThingToStairs, selectedStairs, thing);
                                    }
                                    else
                                    {
                                        ZLogger.Message("Build tree (HaulThingToDest): " + pawn + " - Finding and using " + selectedStairs
                                            + " in " + this.GetMapInfo(map));
                                        gotoStairs = JobMaker.MakeJob(ZLevelsDefOf.ZL_GoToStairs, selectedStairs);
                                    }
                                    tempJobs.Add(gotoStairs);
                                }
                                else
                                {
                                    fail = true;
                                }
                            }
                            else
                            {
                                fail = true;
                            }
                        }
                    }
                }
            }
            return tempJobs;
        }
        public List<Job> GoToMap(Pawn pawn, Map dest, ref IntVec3 lastStairsPosition, ref bool fail)
        {
            List<Job> tempJobs = new List<Job>();
            ZLogger.Message("Build tree: " + pawn + " - GoToMap from " + pawn.Map + " to " + this.GetMapInfo(dest));
            if (pawn.Map != dest)
            {
                if (this.GetZIndexFor(pawn.Map) > this.GetZIndexFor(dest))
                {
                    ZLogger.Message("1 - Build tree (GoToMap): " + pawn + " - Going down");
                    foreach (var map in this.ZLevelsTracker[pawn.Map.Tile].ZLevels.Values.OrderByDescending(x => this.GetZIndexFor(x)))
                    {
                        if (this.GetZIndexFor(pawn.Map) >= this.GetZIndexFor(map) &&
                            this.GetZIndexFor(map) >= this.GetZIndexFor(dest))
                        {
                            if (map != dest)
                            {
                                var stairs = map.listerThings.AllThings.Where(x => x is Building_StairsDown && x.Spawned).ToList();
                                if (stairs?.Count() > 0)
                                {
                                    var selectedStairs = GenClosest.ClosestThing_Global(lastStairsPosition, stairs, 99999f);
                                    if (selectedStairs != null)
                                    {
                                        ZLogger.Message("Build tree (GoToMap): " + pawn + " - Finding and using " + selectedStairs
                                            + " in " + this.GetMapInfo(map));
                                        lastStairsPosition = selectedStairs.Position;
                                        Job goToStairs = JobMaker.MakeJob(ZLevelsDefOf.ZL_GoToStairs, selectedStairs);
                                        tempJobs.Add(goToStairs);
                                    }
                                    else
                                    {
                                        fail = true;
                                    }
                                }
                                else
                                {
                                    fail = true;
                                }
                            }
                        }
                    }
                }
                else if (this.GetZIndexFor(pawn.Map) < this.GetZIndexFor(dest))
                {
                    ZLogger.Message("1 - Build tree (GoToMap): " + pawn + " - Going up");
                    foreach (var map in this.ZLevelsTracker[pawn.Map.Tile].ZLevels.Values.OrderBy(x => this.GetZIndexFor(x)))
                    {
                        if (this.GetZIndexFor(pawn.Map) <= this.GetZIndexFor(map) &&
                            this.GetZIndexFor(map) <= this.GetZIndexFor(dest))
                        {
                            if (map != dest)
                            {
                                var stairs = map.listerThings.AllThings.Where(x => x is Building_StairsUp && x.Spawned).ToList();
                                if (stairs?.Count() > 0)
                                {
                                    var selectedStairs = GenClosest.ClosestThing_Global(lastStairsPosition, stairs, 99999f);
                                    if (selectedStairs != null)
                                    {
                                        ZLogger.Message("Build tree (GoToMap): " + pawn + " - Finding and using " + selectedStairs
                                            + " in " + this.GetMapInfo(map));
                                        lastStairsPosition = selectedStairs.Position;
                                        Job goToStairs = JobMaker.MakeJob(ZLevelsDefOf.ZL_GoToStairs, selectedStairs);
                                        tempJobs.Add(goToStairs);
                                    }
                                    else
                                    {
                                        fail = true;
                                    }
                                }
                                else
                                {
                                    fail = true;
                                }
                            }
                        }
                    }
                }
            }
            return tempJobs;
        }
        public void BuildJobListFor(Pawn pawn, Map start, Map dest, Job jobToDo, Thing thingToHaul)
        {
            this.ResetJobs(pawn);
            List<Job> tempJobs = new List<Job>();
            bool fail = false;
            IntVec3 lastStairsPosition = pawn.Position;
            ZLogger.Message("Starting build tree for " + pawn);
            //Log.Message("jobToDo.def: " + jobToDo.def);
            if (jobToDo.def == JobDefOf.HaulToCell)
            {
                ZLogger.Message("Job method 1");
                tempJobs.AddRange(this.GoToMap(pawn, jobToDo.targetA.Thing.Map, ref lastStairsPosition, ref fail));
                ZLogger.Message(pawn + " haul " + jobToDo.targetA.Thing + " to " + dest);
                tempJobs.AddRange(this.HaulThingToDest(pawn, jobToDo.targetA.Thing, dest, ref lastStairsPosition, ref fail));
                tempJobs.Add(jobToDo);
            }
            else if (jobToDo.def == JobDefOf.HaulToContainer)
            {
                tempJobs.AddRange(this.GoToMap(pawn, jobToDo.targetA.Thing.Map, ref lastStairsPosition, ref fail));
                ZLogger.Message(pawn + " haul " + jobToDo.targetA.Thing + " to " + dest);
                tempJobs.AddRange(this.HaulThingToDest(pawn, jobToDo.targetA.Thing, jobToDo.targetB.Thing.Map, ref lastStairsPosition, ref fail));
                tempJobs.Add(jobToDo);
            }
            else if (jobToDo.def == JobDefOf.Rescue || jobToDo.def == JobDefOf.Capture)
            {
                ZLogger.Message("Job method 1.5: " + jobToDo.targetA.Thing);
                tempJobs.AddRange(this.GoToMap(pawn, jobToDo.targetA.Thing.Map, ref lastStairsPosition, ref fail));
                tempJobs.AddRange(this.HaulThingToDest(pawn, jobToDo.targetA.Thing, jobToDo.targetB.Thing.Map, ref lastStairsPosition, ref fail));
                tempJobs.Add(jobToDo);
            }
            else if (jobToDo?.targetQueueB?.Count > 0)
            {
                foreach (var t in jobToDo.targetQueueB)
                {
                    ZLogger.Message("Job method 2");
                    tempJobs.AddRange(this.GoToMap(pawn, t.Thing.Map, ref lastStairsPosition, ref fail));
                    tempJobs.AddRange(this.HaulThingToDest(pawn, t.Thing, dest, ref lastStairsPosition, ref fail));
                    tempJobs.Add(jobToDo);
                }
            }
            else if (jobToDo.targetA != null && jobToDo.targetA.Thing?.Map != null)
            {
                ZLogger.Message("Job method 3: " + jobToDo.targetA.Thing);
                tempJobs.AddRange(this.GoToMap(pawn, jobToDo.targetA.Thing.Map, ref lastStairsPosition, ref fail));
                tempJobs.Add(jobToDo);
            }
            else if (dest != null)
            {
                ZLogger.Message("Job method 4");
                tempJobs.AddRange(this.GoToMap(pawn, dest, ref lastStairsPosition, ref fail));
                tempJobs.Add(jobToDo);
            }

            if (fail != true && tempJobs.Count > 0)
            {
                this.jobTracker[pawn].activeJobs = tempJobs;
                this.jobTracker[pawn].mainJob = jobToDo;
                this.TryTakeFirstJob(pawn);
            }
            else
            {
                ZLogger.Message("FAIL!!!");
                ZLogger.Message("fail: " + fail);
                ZLogger.Message("tempJobs.Count: " + tempJobs.Count);
                //Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
            }
            ZLogger.Message("Ending build tree for " + pawn);
        }

        public void ResetJobs(Pawn pawn)
        {
            if (this.jobTracker == null)
            {
                ZLogger.Message("Creating new jobTracker");
                this.jobTracker = new Dictionary<Pawn, JobTracker>();
            }
            if (this.jobTracker.ContainsKey(pawn))
            {
                if (this.jobTracker[pawn].activeJobs?.Count() > 0)
                {
                    this.jobTracker[pawn].activeJobs.Clear();
                }
                if (this.jobTracker[pawn].activeJobs == null)
                {
                    this.jobTracker[pawn].activeJobs = new List<Job>();
                }
            }
            else
            {
                ZLogger.Message("Resetting jobTracker for " + pawn);
                this.jobTracker[pawn] = new JobTracker();
                this.jobTracker[pawn].activeJobs = new List<Job>();
            }
        }

        public bool TryTakeFirstJob(Pawn pawn)
        {
            try
            {
                if (this.jobTracker.ContainsKey(pawn) && this.jobTracker[pawn].activeJobs?.Count() > 0)
                {

                    try
                    {
                        foreach (var d in this.jobTracker)
                        {
                            foreach (var t in d.Value.activeJobs)
                            {
                                ZLogger.Message("Active jobs 1: " + d.Key + " - " + t);
                            }
                            foreach (var t in d.Key.jobs.jobQueue)
                            {
                                ZLogger.Message("Active jobQueue 1: " + d.Key + " - " + t.job);
                            }
                            ZLogger.Message("========================");
                        }
                    }
                    catch { }

                    Job job = this.jobTracker[pawn].activeJobs[0];
                    if (job?.def != null)
                    {
                        if (job == this.jobTracker[pawn].mainJob)
                        {
                            if (pawn?.carryTracker?.CarriedThing != null)
                            {
                                ZLogger.Message("this.jobTracker[pawn].mainJob: " + this.jobTracker[pawn].mainJob.targetB.Thing);
                                ZLogger.Message("pawn?.carryTracker?.CarriedThing: " + pawn?.carryTracker?.CarriedThing);
                                Thing newThing;
                                pawn.carryTracker.TryDropCarriedThing
                                (pawn.Position, ThingPlaceMode.Direct, out newThing);
                                ZLogger.Message("newThing: " + newThing);
                                ZLogger.Message("this.jobTracker[pawn].mainJob: " +
                                    this.jobTracker[pawn].mainJob.targetB.Thing);
                                ZLogger.Message("Same things: "
                                    + (newThing == this.jobTracker[pawn].mainJob.targetB.Thing).ToString());
                            }
                            if (job.TryMakePreToilReservations(pawn, false))
                            {
                                ZLogger.Message(pawn + " taking job " + job + " in " + this.GetMapInfo(pawn.Map));
                                pawn.jobs.jobQueue.EnqueueFirst(this.jobTracker[pawn].mainJob);
                            }
                            else
                            {
                                ZLogger.Message(pawn + " in " + this.GetMapInfo(pawn.Map) + " failed " + job);
                            }
                        }
                        else
                        {
                            ZLogger.Message(pawn + " taking job " + job + " in " + this.GetMapInfo(pawn.Map));
                            pawn.jobs.jobQueue.EnqueueFirst(job);
                        }
                        this.jobTracker[pawn].activeJobs.RemoveAt(0);
                    }
                    else
                    {
                        ZLogger.Message("Resetting jobs for " + pawn);
                        this.ResetJobs(pawn);
                        //Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
                    }

                    try
                    {
                        foreach (var d in this.jobTracker)
                        {
                            foreach (var t in d.Value.activeJobs)
                            {
                                ZLogger.Message("Active jobs 2: " + d.Key + " - " + t);
                            }
                            foreach (var t in d.Key.jobs.jobQueue)
                            {
                                ZLogger.Message("Active jobQueue 2: " + d.Key + " - " + t.job);
                            }
                            ZLogger.Message("========================");
                        }
                    }
                    catch { }

                    return true;
                }
            }
            catch (Exception ex)
            {
                ZLogger.Message("Fail in TryTakeFirstJob: " + ex);
            }
            return false;
        }

        public void ResetJobTrackerFor(Pawn pawn)
        {
            if (this.jobTracker.ContainsKey(pawn))
            {
                this.jobTracker[pawn].activeJobs.Clear();
            }
        }

        public void SimpleTeleportThing(Thing thingToTeleport, IntVec3 cellToTeleport, Map mapToTeleport, bool firstTime = false, int dealDamage = 0)
        {
            if (mapToTeleport.thingGrid.ThingsListAt(cellToTeleport).Any())
            {
                for (int i = mapToTeleport.thingGrid.ThingsListAt(cellToTeleport).Count - 1; i >= 0; i--)
                {
                    Thing thing = mapToTeleport.thingGrid.ThingsListAt(cellToTeleport)[i];
                    if (thing is Building)
                    {
                        if (thing.Spawned)
                        {
                            thing.DeSpawn(DestroyMode.Refund);
                        }
                    }
                }
            }

            bool jump = false;
            bool draft = false;
            if (thingToTeleport is Pawn pawnToTeleport)
            {
                if (Find.Selector.SelectedObjects.Contains(pawnToTeleport))
                {
                    jump = true;
                }
                if (pawnToTeleport.Drafted)
                {
                    draft = true;
                }

                try
                {
                    this.SaveArea(pawnToTeleport);
                }
                catch { }
            }

            JobManagerPatches.manualDespawn = true;
            thingToTeleport.DeSpawn();
            JobManagerPatches.manualDespawn = false;
            GenPlace.TryPlaceThing(thingToTeleport, cellToTeleport, mapToTeleport, ThingPlaceMode.Near);

            if (thingToTeleport is Pawn pawnToTeleport2)
            {
                try
                {
                    this.TryTakeFirstJob(pawnToTeleport2);
                }
                catch { };
                try
                {
                    this.LoadArea(pawnToTeleport2);
                }
                catch { }

                if (jump)
                {
                    Current.Game.CurrentMap = mapToTeleport;
                    CameraJumper.TryJumpAndSelect(pawnToTeleport2);
                }
                if (draft)
                {
                    try
                    {
                        pawnToTeleport2.drafter.Drafted = true;
                    }
                    catch { }
                }
            }
            if (dealDamage > 0)
            {
                if (thingToTeleport.def.useHitPoints)
                {
                    thingToTeleport.HitPoints -= dealDamage;
                }
                else if (thingToTeleport is Pawn pawn)
                {
                    pawn.TakeDamage(new DamageInfo(DamageDefOf.Blunt, dealDamage));
                }
            }

            ZLogger.Message("Thing: " + thingToTeleport + " teleported to " + this.GetMapInfo(mapToTeleport) + " - " + cellToTeleport);
            if (firstTime)
            {
                ZLogger.Message("Map: " + mapToTeleport);
                try
                {
                    FloodFillerFog.DebugRefogMap(mapToTeleport);
                    foreach (var cell in mapToTeleport.AllCells)
                    {
                        FloodFillerFog.FloodUnfog(cell, mapToTeleport);
                    }
                }
                catch { };
            }

            FloodFillerFog.FloodUnfog(thingToTeleport.Position, mapToTeleport);

            AccessTools.Method(typeof(FogGrid), "FloodUnfogAdjacent").Invoke(mapToTeleport.fogGrid, new object[]
            { thingToTeleport.PositionHeld });
        }

        public void ZLevelsFixer(int tile)
        {
            if (this.ZLevelsTracker[tile]?.ZLevels[0]?.listerThings == null)
            {
                var map = Find.WorldObjects.MapParents.Where(x => x.Tile == tile 
                && x.HasMap && x.Map.IsPlayerHome).FirstOrDefault().Map;
                this.ZLevelsTracker[tile].ZLevels[0] = map;
            }
        }

        //public void ZLevelsFixer(int tile)
        //{
        //    int num = 0;
        //    while (true && num < 100)
        //    {
        //        var brokenLevel = this.ZLevelsTracker[tile].ZLevels.Where(x => x.Value == null).FirstOrDefault();
        //        if (brokenLevel.Value == null)
        //        {
        //            ZLogger.Message("brokenLevel: " + brokenLevel);
        //            ZLogger.Message("Removing by key: " + brokenLevel.Key);
        //            this.ZLevelsTracker[tile].ZLevels.Remove(brokenLevel.Key);
        //            var newLevels = new Dictionary<int, Map>();
        //        }
        //        else
        //        {
        //            foreach (var map in this.ZLevelsTracker[tile].ZLevels)
        //            {
        //                ZLogger.Message("New level: " + this.GetMapInfo(map.Value));
        //            }
        //            break;
        //        }
        //        num++;
        //    }
        //}

        public void TeleportPawn(Pawn pawnToTeleport, IntVec3 cellToTeleport, Map mapToTeleport, bool firstTime = false, bool spawnStairsBelow = false, bool spawnStairsUpper = false)
        {
            //ZLogger.Message("Trying to teleport to " + mapToTeleport);
            bool jump = false;
            bool draft = false;
            if (Find.Selector.SelectedObjects.Contains(pawnToTeleport))
            {
                jump = true;
            }
            if (pawnToTeleport.Drafted)
            {
                draft = true;
            }
            if (mapToTeleport.thingGrid.ThingsListAt(cellToTeleport).Any())
            {
                for (int i = mapToTeleport.thingGrid.ThingsListAt(cellToTeleport).Count - 1; i >= 0; i--)
                {
                    Thing thing = mapToTeleport.thingGrid.ThingsListAt(cellToTeleport)[i];
                    if (thing is Mineable)
                    {
                        if (thing.Spawned)
                        {
                            thing.DeSpawn(DestroyMode.WillReplace);
                        }
                    }
                }
            }
            var mapComp = mapToTeleport.GetComponent<MapComponentZLevel>();

            //if (mapComp.path != null && mapComp.path.Length > 0)
            //{
            //    mapComp.DoForcedGeneration(true);
            //}

            if (spawnStairsUpper)
            {
                if (this.GetZIndexFor(pawnToTeleport.Map) < this.GetZIndexFor(mapToTeleport))
                {
                    var stairs = this.GetLowerLevel(mapToTeleport.Tile, mapToTeleport)?.thingGrid?
                        .ThingsListAt(cellToTeleport)?.Where(x => x is Building_StairsUp)?.FirstOrDefault();
                    if (stairs != null && stairs.Stuff != null)
                    {
                        var thingToMake = ZLevelsDefOf.ZL_StairsDown;
                        if (cellToTeleport.GetThingList(mapToTeleport).Where(x => x.def == thingToMake).Count() == 0)
                        {
                            var newStairs = ThingMaker.MakeThing(thingToMake, stairs.Stuff);
                            newStairs.SetFaction(stairs.Faction);
                            GenPlace.TryPlaceThing(newStairs, cellToTeleport, mapToTeleport, ThingPlaceMode.Direct);
                        }
                    }
                }
            }
            if (spawnStairsBelow)
            {
                if (this.GetZIndexFor(pawnToTeleport.Map) > this.GetZIndexFor(mapToTeleport))
                {
                    var stairs = pawnToTeleport.Map.thingGrid.ThingsListAt(cellToTeleport)?
                        .Where(x => x is Building_StairsDown)?.FirstOrDefault();
                    ZLogger.Message("Stairs: " + stairs);
                    if (stairs != null)
                    {
                        if (stairs.Stuff != null)
                        {
                            var thingToMake = ZLevelsDefOf.ZL_StairsUp;
                            if (cellToTeleport.GetThingList(mapToTeleport).Where(x => x.def == thingToMake).Count() == 0)
                            {
                                var newStairs = ThingMaker.MakeThing(thingToMake, stairs.Stuff);
                                newStairs.SetFaction(stairs.Faction);
                                GenPlace.TryPlaceThing(newStairs, cellToTeleport, mapToTeleport, ThingPlaceMode.Direct);
                            }
                        }
                        else if (stairs.def.defName == ZLevelsDefOf.ZL_NaturalHole.defName)
                        {
                            foreach (var thing in pawnToTeleport.Map.listerThings.AllThings)
                            {
                                if (thing is Building_StairsDown naturalHole && naturalHole.def.defName == ZLevelsDefOf.ZL_NaturalHole.defName)
                                {
                                    var infestatorsPlace = IntVec3.Invalid;
                                    Thing pawn = null;
                                    if (naturalHole?.infestationData?.infestators != null)
                                    {
                                        Predicate<Thing> validator = delegate (Thing t)
                                        {
                                            return naturalHole.infestationData.infestators.Contains(((Pawn)t).kindDef);
                                        };
                                        pawn = GenClosest.ClosestThing_Global(naturalHole.Position,
                                            mapToTeleport.mapPawns.AllPawns, 99999f, validator);
                                    }
                                    if (pawn != null)
                                    {
                                        infestatorsPlace = pawn.Position;
                                        var tunnel = mapToTeleport.pathFinder.FindPath
                                            (naturalHole.Position, pawn, TraverseParms.For
                                            (TraverseMode.PassAllDestroyableThings, Danger.Deadly),
                                            PathEndMode.OnCell);
                                        if (tunnel?.NodesReversed != null && tunnel.NodesReversed.Count > 0)
                                        {
                                            foreach (var tile in tunnel.NodesReversed)
                                            {
                                                var building = tile.GetFirstBuilding(mapToTeleport);
                                                if (building != null)
                                                {
                                                    building.DeSpawn(DestroyMode.WillReplace);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            //var jobs = pawnToTeleport.jobs.jobQueue.ToList().ListFullCopy();
            try
            {
                this.SaveArea(pawnToTeleport);
            }
            catch { }

            foreach (var animal in pawnToTeleport.relations.DirectRelations
                .Where(x => x.def == PawnRelationDefOf.Bond && x.otherPawn.Spawned
                && pawnToTeleport.Position.InHorDistOf(x.otherPawn.Position, 15)))
            {
                var stairs = pawnToTeleport.Position.GetThingList(pawnToTeleport.Map)
                    .Where(x => x is Building_StairsDown || x is Building_StairsUp).FirstOrDefault();
                if (stairs != null)
                {
                    Job goToStairs = JobMaker.MakeJob(ZLevelsDefOf.ZL_GoToStairs, stairs);
                    animal.otherPawn.jobs.jobQueue.EnqueueFirst(goToStairs);
                }
            }

            JobManagerPatches.manualDespawn = true;
            pawnToTeleport.DeSpawn();
            JobManagerPatches.manualDespawn = false;
            GenPlace.TryPlaceThing(pawnToTeleport, cellToTeleport, mapToTeleport, ThingPlaceMode.Near);

            try
            {
                this.TryTakeFirstJob(pawnToTeleport);
            }
            catch { };
            try
            {
                this.LoadArea(pawnToTeleport);
            }
            catch { }

            ZLogger.Message("Pawn: " + pawnToTeleport + " teleported to " + this.GetMapInfo(mapToTeleport));
            if (jump)
            {
                Current.Game.CurrentMap = mapToTeleport;
                CameraJumper.TryJumpAndSelect(pawnToTeleport);
            }
            if (draft)
            {
                try
                {
                    pawnToTeleport.drafter.Drafted = true;
                }
                catch { }
            }
            if (firstTime)
            {
                try
                {
                    ZLogger.Message("Map: " + mapToTeleport);
                    FloodFillerFog.DebugRefogMap(mapToTeleport);
                    foreach (var cell in mapToTeleport.AllCells)
                    {
                        FloodFillerFog.FloodUnfog(cell, mapToTeleport);
                    }
                }
                catch { };
            }

            FloodFillerFog.FloodUnfog(pawnToTeleport.Position, mapToTeleport);
            AccessTools.Method(typeof(FogGrid), "FloodUnfogAdjacent").Invoke(mapToTeleport.fogGrid, new object[]
            { pawnToTeleport.PositionHeld });

            //foreach (var map2 in Find.Maps)
            //{
            //    var comp2 = map2.GetComponent<MapComponentZLevel>();
            //    if (this.ZLevelsTracker[map2.Tile] != null)
            //    {
            //        foreach (var d in this.ZLevelsTracker[map2.Tile].ZLevels)
            //        {
            //            Log.Message(map2 + ": " + d.Key + " - " + this.GetMapInfo(d.Value));
            //        }
            //    }
            //}

        }

        public Map CreateLowerLevel(Map origin, IntVec3 playerStartSpot)
        {
            var comp = origin.GetComponent<MapComponentZLevel>();
            var mapParent = (MapParent_ZLevel)WorldObjectMaker.MakeWorldObject(ZLevelsDefOf.ZL_Underground);

            mapParent.Tile = origin.Tile;
            mapParent.PlayerStartSpot = playerStartSpot;
            mapParent.TotalInfestations = comp.TotalInfestations;
            mapParent.hasCaves = comp.hasCavesBelow.GetValueOrDefault(false);
            Find.WorldObjects.Add(mapParent);

            string seedString = Find.World.info.seedString;
            Find.World.info.seedString = new System.Random().Next(0, 2147483646).ToString();
            Map newMap = null;
            mapParent.finishedGeneration = false;
            mapParent.Z_LevelIndex = comp.Z_LevelIndex - 1;
            try
            {
                var pathToLoad = Path.Combine(Path.Combine(GenFilePaths.ConfigFolderPath,
                    "SavedMaps"), origin.Tile + " - " + (comp.Z_LevelIndex - 1) + ".xml");
                FileInfo fileInfo = new FileInfo(pathToLoad);
                if (fileInfo.Exists)
                {
                    ZLogger.Message("Loading from " + pathToLoad);
                    newMap = MapGenerator.GenerateMap(origin.Size, mapParent,
                        ZLevelsDefOf.ZL_EmptyMap, mapParent.ExtraGenStepDefs, null);
                    BlueprintUtility.LoadEverything(newMap, pathToLoad);
                }
                else
                {
                    newMap = MapGenerator.GenerateMap(origin.Size, mapParent, mapParent.MapGeneratorDef, mapParent.ExtraGenStepDefs, null);
                }
            }
            catch
            {
                newMap = MapGenerator.GenerateMap(origin.Size, mapParent, mapParent.MapGeneratorDef, mapParent.ExtraGenStepDefs, null);
            }
            mapParent.finishedGeneration = true;

            Find.World.info.seedString = seedString;
            try
            {
                if (this.TryRegisterMap(newMap, comp.Z_LevelIndex - 1))
                {
                    var newComp = newMap.GetComponent<MapComponentZLevel>();
                    newComp.Z_LevelIndex = comp.Z_LevelIndex - 1;
                }
                GameCondition_NoSunlight gameCondition_NoSunlight =
                    (GameCondition_NoSunlight)GameConditionMaker.MakeCondition(ZLevelsDefOf.ZL_UndergroundCondition, -1);
                gameCondition_NoSunlight.Permanent = true;
                newMap.gameConditionManager.RegisterCondition(gameCondition_NoSunlight);
            }
            catch
            {

            }
            return newMap;
        }

        public Map CreateUpperLevel(Map origin, IntVec3 playerStartSpot)
        {
            var mapParent = (MapParent_ZLevel)WorldObjectMaker.MakeWorldObject(ZLevelsDefOf.ZL_Upper);

            var comp = origin.GetComponent<MapComponentZLevel>();

            mapParent.Tile = origin.Tile;
            mapParent.PlayerStartSpot = playerStartSpot;
            mapParent.hasCaves = false;
            Find.WorldObjects.Add(mapParent);

            string seedString = Find.World.info.seedString;
            Find.World.info.seedString = new System.Random().Next(0, 2147483646).ToString();
            Map newMap = null;
            mapParent.finishedGeneration = false;
            mapParent.Z_LevelIndex = comp.Z_LevelIndex + 1;
            try
            {
                var pathToLoad = Path.Combine(Path.Combine(GenFilePaths.ConfigFolderPath,
                    "SavedMaps"), origin.Tile + " - " + (comp.Z_LevelIndex + 1) + ".xml");
                FileInfo fileInfo = new FileInfo(pathToLoad);
                if (fileInfo.Exists)
                {
                    ZLogger.Message("Loading from " + pathToLoad);
                    newMap = MapGenerator.GenerateMap(origin.Size, mapParent, ZLevelsDefOf.ZL_EmptyMap
                        , mapParent.ExtraGenStepDefs, null);
                    BlueprintUtility.LoadEverything(newMap, pathToLoad);
                }
                else
                {
                    newMap = MapGenerator.GenerateMap(origin.Size, mapParent, mapParent.MapGeneratorDef,
                        mapParent.ExtraGenStepDefs, null);
                }
            }
            catch
            {
                newMap = MapGenerator.GenerateMap(origin.Size, mapParent, mapParent.MapGeneratorDef,
                    mapParent.ExtraGenStepDefs, null);
            };
            mapParent.finishedGeneration = true;
            Find.World.info.seedString = seedString;
            try
            {
                if (this.TryRegisterMap(newMap, comp.Z_LevelIndex + 1))
                {
                    var newComp = newMap.GetComponent<MapComponentZLevel>();
                    newComp.Z_LevelIndex = comp.Z_LevelIndex + 1;
                    AdjustMapGeneration(newMap);
                }
            }
            catch
            {

            }
            try
            {
                newMap.terrainGrid.SetTerrain(playerStartSpot, ZLevelsDefOf.ZL_OutsideTerrainTwo);
            }
            catch { }

            foreach (var intVec in newMap.AllCells)
            {
                newMap.mapDrawer.MapMeshDirty(intVec, MapMeshFlag.Terrain);
                newMap.mapDrawer.MapMeshDirty(intVec, MapMeshFlag.Things);
                newMap.mapDrawer.MapMeshDirty(intVec, MapMeshFlag.FogOfWar);
            }
            return newMap;
        }

        private bool SurroundedWithRock(IntVec3 c, Map map)
        {
            int num = 0;
            foreach (var cell in GenAdj.CellsAdjacentCardinal(c, Rot4.South, new IntVec2(1, 1)))
            {
                if (GenGrid.InBounds(cell, map) && cell.GetFirstBuilding(map) is Mineable)
                {
                    num++;
                    if (num >= 2)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public void AdjustMapGeneration(Map map)
        {
            try
            {
                Map mapBelow = this.GetLowerLevel(map.Tile, map);
                RockNoises.Init(map);
                foreach (IntVec3 allCell in map.AllCells)
                {
                    TerrainDef terrainDef = null;
                    if (mapBelow.roofGrid.RoofAt(allCell) != null && !mapBelow.roofGrid.RoofAt(allCell).isNatural)
                    {
                        terrainDef = ZLevelsDefOf.ZL_RoofTerrain;
                    }
                    else if (mapBelow.roofGrid.RoofAt(allCell) != null
                        && mapBelow.roofGrid.RoofAt(allCell).isNatural)
                    {
                        if (allCell.GetEdifice(mapBelow) is Mineable rock && rock.def.building.isNaturalRock
                            && !rock.def.building.isResourceRock)
                        {
                            terrainDef = TerrainDef.Named(rock.def.defName + "_Rough");
                            if (terrainDef == null)
                            {
                                terrainDef = TerrainDef.Named("Granite_Rough");
                            }
                        }
                        else
                        {
                            terrainDef = TerrainDef.Named("Granite_Rough");
                        }
                    }
                    if (allCell.GetEdifice(mapBelow) is Mineable rock2 && rock2.Spawned && !rock2.Destroyed
                        && mapBelow.roofGrid.RoofAt(allCell) != null
                        && (mapBelow.roofGrid.RoofAt(allCell) == RoofDefOf.RoofRockThick
                        || mapBelow.roofGrid.RoofAt(allCell) == RoofDefOf.RoofRockThin))
                    {
                        try
                        {
                            try
                            {
                                if (terrainDef == ZLevelsDefOf.ZL_RoofTerrain)
                                {
                                    terrainDef = rock2.def.building.naturalTerrain;
                                }
                            }
                            catch { };
                            GenSpawn.Spawn(rock2.def, allCell, map);
                            if (SurroundedWithRock(allCell, map))
                            {
                                map.roofGrid.SetRoof(allCell, allCell.GetRoof(mapBelow));
                            }
                            else
                            {
                                map.roofGrid.SetRoof(allCell, null);
                            }
                        }
                        catch { };

                    }
                    if (terrainDef != null)
                    {
                        map.terrainGrid.SetTerrain(allCell, terrainDef);
                    }
                }
                GenStep_ScatterLumpsMineableUnderground genStep_ScatterLumpsMineable = new GenStep_ScatterLumpsMineableUnderground();
                genStep_ScatterLumpsMineable.maxValue = float.MaxValue;
                float num3 = 15f;
                genStep_ScatterLumpsMineable.countPer10kCellsRange = new FloatRange(num3, num3);
                genStep_ScatterLumpsMineable.Generate(map, default(GenStepParams));
            }
            catch { };
        }

        public override void StartedNewGame()
        {
            base.StartedNewGame();
        }

        public override void LoadedGame()
        {
            base.LoadedGame();
            try
            {
                foreach (var test in this.ZLevelsTracker)
                {
                    Log.Message("Z-Levels start");
                    foreach (var d in test.Value.ZLevels)
                    {
                        if (d.Key < 0)
                        {
                            Log.Message("Registered map: " + this.GetMapInfo(d.Value) + " - Underground");
                        }
                        else if (d.Key > 0)
                        {
                            Log.Message("Registered map: " + this.GetMapInfo(d.Value) + " - Upper level");
                        }
                        else
                        {
                            Log.Message("Registered map: " + this.GetMapInfo(d.Value) + " - Normal player map");
                        }
                    }
                }
            }
            catch { };
            try
            {
                foreach (var pawn in PawnsFinder.AllMaps)
                {
                    if (pawn.Spawned && pawn.Map != null)
                    {
                        if (this.GetZIndexFor(pawn.Map) < 0)
                        {
                            Log.Message("Registered map: " + this.GetMapInfo(pawn.Map) + " - Underground");
                        }
                        else if (this.GetZIndexFor(pawn.Map) > 0)
                        {
                            Log.Message("Registered map: " + this.GetMapInfo(pawn.Map) + " - Upper level");
                        }
                        else
                        {
                            Log.Message("Registered map: " + this.GetMapInfo(pawn.Map) + " - Normal player map");
                        }
                    }
                }
            }
            catch { };
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look<Pawn, ActiveArea>(ref this.activeAreas, "activeAreas", LookMode.Reference,
                LookMode.Deep, ref this.PawnKeys, ref this.ActiveAreasValues);
            Scribe_Collections.Look<Pawn, JobTracker>(ref this.jobTracker, "jobTracker", LookMode.Reference,
                LookMode.Deep, ref this.PawnKeys, ref this.JobTrackerValues);
            Scribe_Collections.Look<int, ZLevelData>(ref this.ZLevelsTracker, "ZLevelsTracker",
                LookMode.Value, LookMode.Deep, ref this.Z_LevelsKeys, ref this.ZLevelsTrackerValues);
        }

        public Dictionary<Pawn, ActiveArea> activeAreas;
        public List<Pawn> PawnKeys = new List<Pawn>();
        public List<ActiveArea> ActiveAreasValues = new List<ActiveArea>();

        public Dictionary<Pawn, JobTracker> jobTracker;
        public List<JobTracker> JobTrackerValues = new List<JobTracker>();

        public List<int> Z_LevelsKeys = new List<int>();
        public Dictionary<int, ZLevelData> ZLevelsTracker = new Dictionary<int, ZLevelData>();
        public List<ZLevelData> ZLevelsTrackerValues = new List<ZLevelData>();
    }
}

