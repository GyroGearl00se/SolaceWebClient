using SolaceSystems.Solclient.Messaging;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ISession = SolaceSystems.Solclient.Messaging.ISession;
using SolaceSystems.Solclient.Messaging.SDT;

namespace SolaceWebClient.Services
{
    public class MessageDetails
    {
        public string DestinationName { get; set; }
        public string ApplicationMessageId { get; set; }
        public string SenderId { get; set; }
        public string MessageContent { get; set; }
        public string MessageContentXML { get; set; }
        public string ApplicationMessageType { get; set; }
        public string CorrelationId { get; set; }
        public long ADMessageId { get; set; }
        public string FormattedDateTime { get; set; }
        public Dictionary<string, object> UserProperties { get; set; }
        public string DeliveryMode { get; set; }
        public int Size { get; set; }
    }

    public class QueueBrowserService
    {
        private IContext _context;
        private ISession _session;
        private readonly ILogger<QueueBrowserService> _logger;

        public QueueBrowserService(ILogger<QueueBrowserService> logger)
        {
            _logger = logger;

            ContextFactoryProperties cfp = new ContextFactoryProperties()
            {
                SolClientLogLevel = SolLogLevel.Warning
            };
            cfp.LogToConsoleError();
            ContextFactory.Instance.Init(cfp);
            _context = ContextFactory.Instance.CreateContext(new ContextProperties(), null);
        }

        private void Disconnect()
        {
            if (_session != null)
            {
                _logger.LogInformation("Disconnecting session.");
                _session.Disconnect();
                _session.Dispose();
                _session = null;
            }
        }

        

        public async Task<List<MessageDetails>> BrowseQueueAsync(string host, string vpnName, string username, string password, string queueName, bool sslVerify, int maxMessages)
        {
            _logger.LogInformation("Queue Browsing started.");
            List<MessageDetails> messages = new List<MessageDetails>();
            try
            {
                SessionProperties sessionProps = new SessionProperties()
                {
                    Host = host,
                    VPNName = vpnName,
                    UserName = username,
                    Password = password,
                    SSLValidateCertificate = sslVerify,
                    SSLTrustStoreDir = "trustedca"
                };

                _session = _context.CreateSession(sessionProps, null, null);
                ReturnCode returnCode = _session.Connect();
                if (returnCode != ReturnCode.SOLCLIENT_OK)
                {
                    _logger.LogError("Failed to connect to Solace broker with return code {returnCode}", returnCode);
                    throw new Exception("Failed to connect to Solace broker.");
                }

                _logger.LogInformation("Browsing queue: {queueName}", queueName);
                using (IQueue queue = ContextFactory.Instance.CreateQueue(queueName))
                {
                    BrowserProperties browserProps = new BrowserProperties()
                    {
                        MaxReconnectTries = 5
                    };

                    using (IBrowser browser = _session.CreateBrowser(queue, browserProps))
                    {
                        IMessage message;
                        int messageCount = 0;
                        while ((message = await Task.Run(() => browser.GetNext())) != null && messageCount < maxMessages)
                        {
                            //_logger.LogInformation("Message received: {message}", Encoding.UTF8.GetString(message.BinaryAttachment));

                            string formattedDateTime;

                            if (message.SenderTimestamp == -1)
                            {
                                formattedDateTime = "";
                            } else
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
                            Console.WriteLine($"Message ID: {message.ADMessageId}");
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

                            Console.WriteLine($"################# Message size: {messageSize.ToString("N0")} bytes");
                            messages.Add(new MessageDetails
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
                            messageCount++;
                        }
                    }
                }
            }
            catch (OperationErrorException ex)
            {
                _logger.LogError("OperationErrorException: {ex}", ex);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception: {ex}", ex);
                throw;
            }
            finally
            {
                Disconnect();
            }

            return messages;
        }

        public async Task DeleteMessage(string host, string vpnName, string username, string password, string queueName, bool sslVerify, long adMessageId)
        {
            _logger.LogInformation("Delete Message process started.");
            try
            {
                SessionProperties sessionProps = new SessionProperties()
                {
                    Host = host,
                    VPNName = vpnName,
                    UserName = username,
                    Password = password,
                    ReconnectRetries = 3,
                    SSLValidateCertificate = sslVerify,
                    SSLTrustStoreDir = "trustedca"
                };

                _session = _context.CreateSession(sessionProps, null, null);
                ReturnCode returnCode = _session.Connect();
                if (returnCode != ReturnCode.SOLCLIENT_OK)
                {
                    _logger.LogError("Failed to connect to Solace broker with return code {returnCode}", returnCode);
                    throw new Exception("Failed to connect to Solace broker.");
                }

                using (IQueue queue = ContextFactory.Instance.CreateQueue(queueName))
                {
                    BrowserProperties browserProps = new BrowserProperties()
                    {
                        TransportWindowSize = 5,
                        MaxReconnectTries = 5
                    };

                    using (IBrowser browser = _session.CreateBrowser(queue, browserProps))
                    {
                        browser.Remove(adMessageId);
                    }
                }
            }
            catch (OperationErrorException ex)
            {
                _logger.LogError("OperationErrorException: {ex}", ex);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception: {ex}", ex);
                throw;
            }
            finally
            {
                Disconnect();
            }
        }
    }
}
