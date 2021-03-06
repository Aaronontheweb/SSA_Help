﻿using Akka.Actor;
using Akka.Event;
using EY.SSA.CommonBusinessLogic.BridgeInterfaces;
using EY.SSA.CommonBusinessLogic.General;
using EY.SSA.CommonBusinessLogic.Messages.Actions;
using EY.SSA.CommonBusinessLogic.Messages.Commands;
using EY.SSA.CommonBusinessLogic.Messages.Events;
using EY.SSA.CommonBusinessLogic.Messages.Requests;
using EY.SSA.CommonBusinessLogic.State;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace EY.SSA.CommonBusinessLogic.Actors.Interfaces
{
    public class UserExternalMessageToAkkaMessageTranslator : IExternalActionMessageToAkkaMessageHandler
    {
        private Dictionary<string, List<MicroServices.Area>> _ConnectionAreas;

        public IActorRef SendTo { get; set; }

        public IActorRef ReplyTo { get; set; }

        public ILoggingAdapter Logger { get; set; }

        // This is how we call methods on the http client
        private IHTTPExternalInterface HTTPExternalInterface { get; set; }

        public MicroServices.Area Area { get; } = MicroServices.Area.User;

        public UserExternalMessageToAkkaMessageTranslator(IHTTPExternalInterface httpExternalInterface, IActorRef sendTo, IActorRef replyTo, ILoggingAdapter logger)
        {
            SendTo = sendTo;
            ReplyTo = replyTo;
            Logger = logger;
            HTTPExternalInterface = httpExternalInterface;
        }


        public bool ProcessCommand(HTTPSourcedCommand externalCommand)
        {
            MicroServices.CommandType cType;

            bool ableToParseCommand = MicroServices.ParseCommandType(externalCommand.CommandType, out cType);

            // Handle Command
            if (ableToParseCommand)
            {
                Logger.Debug($"{Area}HTTP Bridge Converted Command'{ externalCommand.CommandType}' to internal value{cType.ToString()}.");
                TrackConnection(externalCommand);

                switch (cType)
                {
                    //Translate and Send message to user admin actor so that he will forward it to the user actor
                    case MicroServices.CommandType.Insert:
                        TranslateExternalInsertCommandToAkkaMessage(externalCommand);
                        break;
                    case MicroServices.CommandType.Update:
                        TranslateExternalUpdateCommandToAkkaMessage(externalCommand);
                        break;
                    case MicroServices.CommandType.Upsert:
                        TranslateExternalUpsertCommandToAkkaMessage(externalCommand);
                        break;
                    case MicroServices.CommandType.Delete:
                        TranslateExternalDeleteCommandToAkkaMessage(externalCommand);
                        break;
                    case MicroServices.CommandType.Undelete:
                        TranslateExternalUnDeleteCommandToAkkaMessage(externalCommand);
                        break;
                }
            }
            else
            {
                Logger.Debug($"HTTP User Bridge received unknown string command value'{externalCommand.CommandType}'.");
                HTTPExternalInterface.HandleFailedStateMessage(new HTTPDestinedCommandStateEvent(MicroServices.ProcessingStatus.Failed, $"Unknown command:{externalCommand.CommandType}",externalCommand), true);
            }
            return ableToParseCommand;
        }


        public void TranslateExternalDeleteCommandToAkkaMessage(HTTPSourcedCommand cmdExternal)
        {
            JObject jo = cmdExternal.Data as JObject;
            string id = jo.Value<string>("Id")?? jo.Value<string>("id");
            UserDeleteCommand deleteCmd = new UserDeleteCommand(id, cmdExternal.User, cmdExternal.ConnectionId);
            SendTo.Tell(deleteCmd, ReplyTo);
        }
        public void TranslateExternalUnDeleteCommandToAkkaMessage(HTTPSourcedCommand cmdExternal)
        {
            JObject jo = cmdExternal.Data as JObject;
            string id = jo.Value<string>("Id") ?? jo.Value<string>("id");
            UserUnDeleteCommand deleteCmd = new UserUnDeleteCommand(id, cmdExternal.User, cmdExternal.ConnectionId);
            SendTo.Tell(deleteCmd, ReplyTo);
        }

        public void TranslateExternalInsertCommandToAkkaMessage(HTTPSourcedCommand cmdExternal)
        {
            UserState cs;
            if(ExtractStateObject(cmdExternal,out cs))
            {
                UserInsertCommand insertCommand = new UserInsertCommand(cs, cmdExternal.User, cmdExternal.ConnectionId);
                SendTo.Tell(insertCommand, ReplyTo);
            }
        }

        public void TranslateExternalUpdateCommandToAkkaMessage(HTTPSourcedCommand cmdExternal)
        {
            UserState cs;
            if (ExtractStateObject(cmdExternal, out cs))
            {
                UserUpdateCommand updateCmd = new UserUpdateCommand(cs, cmdExternal.User, cmdExternal.ConnectionId);
                SendTo.Tell(updateCmd, ReplyTo);
            }
        }

        public void TranslateExternalUpsertCommandToAkkaMessage(HTTPSourcedCommand cmdExternal)
        {
            UserState cs;
            if(ExtractStateObject(cmdExternal,out cs))
            {
                UserUpsertCommand upsertCmd = new UserUpsertCommand(cs, cmdExternal.User, cmdExternal.ConnectionId);
                SendTo.Tell(upsertCmd, ReplyTo);
            }
        }


        private bool ExtractStateObject(HTTPSourcedCommand c, out UserState cs)
        {
            try
            {
                JObject jo = c.Data as JObject;
                cs = UserState.InstantiateUserState(jo);
                return true;
            }
            catch(Exception ex)
            {
                cs = null;
                Logger.Error($"During '{c.CommandType}' system was unable to handle user's information. Exception:{ex.ToString()}");
                HTTPExternalInterface.HandleFailedStateMessage(new HTTPDestinedCommandStateEvent(MicroServices.ProcessingStatus.Failed, $"During '{c.CommandType}' system was unable to handle user's information.", c), true);
            }
            return false;
        }


        public bool ProcessRequest(HTTPSourcedRequest externalRequest)
        {
            MicroServices.RequestType rType;
            bool ableToParseRequest = MicroServices.ParseRequestType(externalRequest.RequestType, out rType);

            // Handle Command
            if (ableToParseRequest)
            {
                Logger.Debug($"HTTP User Bridge Converted Request'{externalRequest.RequestType}' to internal value '{rType.ToString()}'.");
                TrackConnection(externalRequest);

                switch (rType)
                {
                    case MicroServices.RequestType.GetList:
                        {
                            TranslateExternalGetListRequestToAkkaMessage(externalRequest);
                            break;
                        }
                    default:
                        {
                            throw new NotImplementedException($"This request type has not been implemented:{rType.ToString()}");
                        }
                }


            }
            else
            {

                Logger.Debug($"HTTP User Bridge received unknown request value'{externalRequest.RequestType}'.");
                HTTPExternalInterface.HandleFailedStateMessage(new HTTPDestinedRequestStateEvent(MicroServices.ProcessingStatus.Failed, "Unknown request:" + externalRequest.RequestType, externalRequest), true);
            }

            return ableToParseRequest;

        }

        public void TranslateExternalGetListRequestToAkkaMessage(HTTPSourcedRequest externalRequest)
        {
            UserGetListRequest getListRequest = new UserGetListRequest(ReplyTo, externalRequest);
            SendTo.Tell(getListRequest, ReplyTo);
        }


        private void TrackConnection(HTTPSourcedAction a)
        {
            _ConnectionAreas = _ConnectionAreas ?? new Dictionary<string, List<MicroServices.Area>>();
            // Add the connection to the dict if not there
            if (!_ConnectionAreas.ContainsKey(a.ConnectionId))
            {
                _ConnectionAreas.Add(a.ConnectionId, new List<MicroServices.Area>());
            }
            // Add the area and add the user to the signal r group if not there
            if (!_ConnectionAreas[a.ConnectionId].Contains(MicroServices.Area.User))
            {
                Logger.Debug($"Adding user '{a.User}' to group '{Area.ToString()}'");
                _ConnectionAreas[a.ConnectionId].Add(Area);
                HTTPExternalInterface.JoinGroup(Area.ToString(), a.ConnectionId);
            }

        }

        public bool ProcessHTTPClientRemoval(RemoveHTTPClient e)
        {
            _ConnectionAreas.Remove(e.ConnectionId);
            return true;
        }

    }
}
