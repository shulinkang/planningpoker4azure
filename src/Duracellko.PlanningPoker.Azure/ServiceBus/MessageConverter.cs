﻿using System;
using System.Text;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;

namespace Duracellko.PlanningPoker.Azure.ServiceBus
{
    /// <summary>
    /// Instance of this class is able to convert messages of type <see cref="T:NodeMessage"/> to BrokeredMessage and vice versa.
    /// </summary>
    public class MessageConverter : IMessageConverter
    {
        /// <summary>
        /// Name of property in BrokeredMessage holding recipient node ID.
        /// </summary>
        internal const string RecipientIdPropertyName = "RecipientId";

        /// <summary>
        /// Name of property in BrokeredMessage holding sender node ID.
        /// </summary>
        internal const string SenderIdPropertyName = "SenderId";

        private const string MessageTypePropertyName = "MessageType";
        private const string MessageSubtypePropertyName = "MessageSubtype";

        /// <summary>
        /// Converts <see cref="T:NodeMessage"/> message to BrokeredMessage object.
        /// </summary>
        /// <param name="message">The message to convert.</param>
        /// <returns>Converted message of BrokeredMessage type.</returns>
        public Message ConvertToBrokeredMessage(NodeMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            string messageData;
            if (message.MessageType == NodeMessageType.InitializeTeam || message.MessageType == NodeMessageType.TeamCreated)
            {
                messageData = (string)message.Data;
            }
            else
            {
                messageData = JsonConvert.SerializeObject(message.Data);
            }

            var result = new Message(Encoding.UTF8.GetBytes(messageData));
            result.UserProperties[MessageTypePropertyName] = message.MessageType.ToString();
            if (message.Data != null)
            {
                result.UserProperties[MessageSubtypePropertyName] = message.Data.GetType().Name;
            }

            result.UserProperties[SenderIdPropertyName] = message.SenderNodeId;
            result.UserProperties[RecipientIdPropertyName] = message.RecipientNodeId;
            return result;
        }

        /// <summary>
        /// Converts BrokeredMessage message to <see cref="T:NodeMessage"/> object.
        /// </summary>
        /// <param name="message">The message to convert.</param>
        /// <returns>Converted message of NodeMessage type.</returns>
        public NodeMessage ConvertToNodeMessage(Message message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var messageType = (NodeMessageType)Enum.Parse(typeof(NodeMessageType), (string)message.UserProperties[MessageTypePropertyName]);
            var messageSubtype = message.UserProperties.ContainsKey(MessageSubtypePropertyName) ? (string)message.UserProperties[MessageSubtypePropertyName] : null;

            var result = new NodeMessage(messageType);
            result.SenderNodeId = (string)message.UserProperties[SenderIdPropertyName];
            result.RecipientNodeId = (string)message.UserProperties[RecipientIdPropertyName];

            var messageJson = message.Body != null ? Encoding.UTF8.GetString(message.Body) : null;
            switch (result.MessageType)
            {
                case NodeMessageType.ScrumTeamMessage:
                    if (string.Equals(messageSubtype, typeof(ScrumTeamMemberMessage).Name, StringComparison.OrdinalIgnoreCase))
                    {
                        result.Data = JsonConvert.DeserializeObject<ScrumTeamMemberMessage>(messageJson);
                    }
                    else if (string.Equals(messageSubtype, typeof(ScrumTeamMemberEstimationMessage).Name, StringComparison.OrdinalIgnoreCase))
                    {
                        result.Data = JsonConvert.DeserializeObject<ScrumTeamMemberEstimationMessage>(messageJson);
                    }
                    else
                    {
                        result.Data = JsonConvert.DeserializeObject<ScrumTeamMessage>(messageJson);
                    }

                    break;
                case NodeMessageType.TeamCreated:
                case NodeMessageType.InitializeTeam:
                    result.Data = messageJson;
                    break;
                case NodeMessageType.TeamList:
                case NodeMessageType.RequestTeams:
                    result.Data = JsonConvert.DeserializeObject<string[]>(messageJson);
                    break;
            }

            return result;
        }
    }
}
