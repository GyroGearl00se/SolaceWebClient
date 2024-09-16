using Microsoft.Extensions.Logging;
using SolaceSystems.Solclient.Messaging;
using SolaceSystems.Solclient.Messaging.SDT;
using System;
using System.Text;
using System.Threading.Tasks;
using ISession = SolaceSystems.Solclient.Messaging.ISession;

namespace SolaceWebClient.Services
{
    public class SolaceSubscribeService : IDisposable
    {
        private IContext _context;
        private ISession _session;
        private readonly ILogger<SolaceSubscribeService> _logger;

        public SolaceSubscribeService(ILogger<SolaceSubscribeService> logger)
        {
            _logger = logger;
            
        }

        public void SubscribeToTopic(string host, string vpnName, string username, string password, string topic, bool sslVerify, Action<MessageDetails> messageHandler)
        {
            try
            {
                if (_session != null)
                {
                    Disconnect();
                }

                ContextFactoryProperties cfp = new ContextFactoryProperties()
                {
                    SolClientLogLevel = SolLogLevel.Warning
                };
                cfp.LogToConsoleError();
                ContextFactory.Instance.Init(cfp);
                _context = ContextFactory.Instance.CreateContext(new ContextProperties(), null);

                SessionProperties sessionProps = new SessionProperties()
                {
                    Host = host,
                    VPNName = vpnName,
                    UserName = username,
                    Password = password,
                    SSLValidateCertificate = sslVerify,
                    SSLTrustStoreDir = "trustedca"
                };

                _session = _context.CreateSession(sessionProps, (source, args) => HandleMessage(source, args, messageHandler), null);
                ReturnCode returnCode = _session.Connect();
                if (returnCode != ReturnCode.SOLCLIENT_OK)
                {
                    throw new Exception("Failed to connect to Solace broker.");
                }
                _session.Subscribe(ContextFactory.Instance.CreateTopic(topic), true);
            }
            catch (OperationErrorException ex)
            {
                _logger.LogError("Exception during SubscribeToTopic: {ex}", ex.ErrorInfo);
                throw;
            }
        }

        public void HandleMessage(object source, MessageEventArgs args, Action<MessageDetails> messageHandler)
        {
            try
            {
                using (IMessage message = args.Message)
                {
                    string formattedDateTime;

                    if (message.SenderTimestamp == -1)
                    {
                        formattedDateTime = "";
                    }
                    else
                    {
                        DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(message.SenderTimestamp);
                        formattedDateTime = dateTimeOffset.ToString("yyyy-MM-dd HH:mm:ss");
                    }

                    Dictionary<string, object> keyValuePairs = new Dictionary<string, object>();

                    var userPropertyMap = message.UserPropertyMap;
                    if (userPropertyMap != null)
                    {
                        while (true)
                        {
                            var nextKeyValuePair = userPropertyMap.GetNext();
                            if (nextKeyValuePair.Key == null)
                            {
                                break;
                            }
                            var key = nextKeyValuePair.Key;
                            var valueObject = nextKeyValuePair.Value;
                            var value = ((ISDTField)valueObject).Value;
                            keyValuePairs.Add(key, value);
                        }
                    }

                    string messageContent;
                    if (SDTUtils.GetText(message) != null)
                    {
                        messageContent = SDTUtils.GetText(message);
                    }
                    else
                    {
                        byte[] binaryAttachment = message.BinaryAttachment;
                        if (binaryAttachment != null && binaryAttachment.Length > 0)
                        {
                            messageContent = Encoding.UTF8.GetString(binaryAttachment);
                        }
                        else
                        {
                            messageContent = "";
                        }
                    }

                    int messageSize = 0;
                    if (message.BinaryAttachment != null)
                    {
                        byte[] binaryAttachment = message.BinaryAttachment;
                        messageSize = binaryAttachment.Length;
                    }
                    else if (message.XmlContent != null)
                    {
                        messageSize = message.XmlContent.Length;
                    }
                    else if (SDTUtils.GetText(message) != null)
                    {
                        messageSize = SDTUtils.GetText(message).Length;
                    }

                    messageHandler(new MessageDetails
                    {
                        DestinationName = message.Destination.Name != null ? message.Destination.Name : "",
                        ApplicationMessageType = message.ApplicationMessageType != null ? message.ApplicationMessageType : "",
                        ApplicationMessageId = message.ApplicationMessageId != null ? message.ApplicationMessageId : "",
                        SenderId = message.SenderId != null ? message.SenderId : "",
                        MessageContent = messageContent,
                        MessageContentXML = message.XmlContent != null ? System.Text.Encoding.ASCII.GetString(message.XmlContent) : "",
                        CorrelationId = message.CorrelationId != null ? message.CorrelationId : "",
                        ADMessageId = message.ADMessageId != 0 ? message.ADMessageId : 0,
                        FormattedDateTime = formattedDateTime,
                        UserProperties = keyValuePairs,
                        DeliveryMode = message.DeliveryMode.ToString(),
                        Size = messageSize
                    });
                }
            }
            catch (OperationErrorException ex)
            {
                _logger.LogError("Exception during HandleMessage: {ex}", ex);
                throw;
            }
        }

        public void Disconnect()
        {
            if (_session != null)
            {
                _session.Disconnect();
                _session.Dispose();
                _session = null;
            }

            if (_context != null)
            {
                _context.Dispose();
            }
        }

        public void Dispose()
        {
            Disconnect();
        }
    }
}
