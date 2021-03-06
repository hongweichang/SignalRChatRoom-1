﻿using System;
using System.Linq;
using System.Timers;
using System.Web;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;
using NLog;

namespace SignalRChat.Common
{
    [JsonObject]
    public class GameGroup
    {


        public string id { get; private set; }
        public int countdownTime { get; private set; }
        //private Stopwatch stopwatch = new Stopwatch();
        private object _lock = new object(); 
        public ConcurrentDictionary<string, PlayerState> PlayerStates = new ConcurrentDictionary<string, PlayerState>();


        public GameGroup(string idIn) {
            this.id = idIn;
            this.countdownTime = 5;
            //this.stopwatch.Start();
        }


        public void DecrementTimer(){
            this.countdownTime--; //only accessed by 1 thread no need for Thread Safety
        }

        //Check for once all the clients have sent there latest time then send an update to all clients
        public bool IsDownloadReady() {
            bool allready = true;
            //ChatHub.logger.Debug("------------------ Start ----------------");
            foreach (var item in PlayerStates)
            {
                if (PlayerStates[item.Key].playerLeftGame)
                {
                    
                }
                else if(!PlayerStates[item.Key].SentLatestFlagRead() ) { // one of the flags is false so still waiting
                    //ChatHub.logger.Debug("is false, so shouldn't return True!!");
                    allready = false; 
                }
            }
            //ChatHub.logger.Debug("------------------ Return {0} ----------------", allready);
            return allready;
        }

        //Once the download sent reset the sent flag so the process can be repeated
        public void ResetSents() {
            //ChatHub.logger.Debug("------------ Start Resetting ------------ ");
            foreach (var item in PlayerStates)
            {
                PlayerStates[item.Key].resetSentLatestFlagToFalse(); //Threadsafe uses interlocking
                //PlayerStates[item.Key].SentLatestFlag = false;
                //ChatHub.logger.Debug(" Reset flag : {0} ", PlayerStates[item.Key].SentLatestFlag);
            }
            //ChatHub.logger.Debug("------------ End Resetting ------------ ");

            //measuring time of round trip
            //this.stopwatch.Stop();
            //send to logger??
            //ChatHub.logger.Debug("Group id : {0}   , duration MS : {1}", this.id, this.stopwatch.ElapsedMilliseconds.ToString());
            //this.stopwatch.Reset();
            //this.stopwatch.Restart();
        }

        public void UpdateState(PlayerState playerState, SignalRChat.ChatHub hub)
        {
            lock (_lock)
            {
                this.PlayerStates[playerState.Id].UpdateClicks(playerState); //Interlocking so is Threadsafe 
                this.PlayerStates[playerState.Id].SentLatestFlag = true;
                //update sent flag duh!

                if (this.IsDownloadReady()) //uses Interlocking to check playerstate dependecy flag 
                {
                    //GameGroup gg = GameGroups[playerState.GroupId];

                    //ChatHub.DebugOut("GameGroups id : " + gg.id);

                    //Clients.Group(playerState.GroupId).UpdateGame(gg); //sends group back to players in group
                    hub.Clients.Group(playerState.GroupId).UpdateGame(this);

                    this.ResetSents();
                }
            }

        }


    }
}