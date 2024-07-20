using Microsoft.Extensions.Logging;
using SolaceSystems.Solclient.Messaging;
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

        public void SubscribeToTopic(string host, string vpnName, string username, string password, string topic, bool sslVerify, Action<string> messageHandler)
        {
            try
            {
                ContextFactoryProperties cfp = new ContextFactoryProperties()
                {
                    SolClientLogLevel = SolLogLevel.Warning
                };
                cfp.LogToConsoleError();
                ContextFactory.Instance.Init(cfp);
                _context = ContextFactory.Instance.CreateContext(new ContextProperties(), null);

                if (_session != null)
                {
                    Disconnect();
                }

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

        public void HandleMessage(object source, MessageEventArgs args, Action<string> messageHandler)
        {
            try
            {
                using (IMessage message = args.Message)
                {
                    string messageContent;

                    byte[] binaryAttachment = message.BinaryAttachment;
                    if (binaryAttachment != null && binaryAttachment.Length > 0)
                    {
                        messageContent = Encoding.UTF8.GetString(binaryAttachment);
                        messageHandler(messageContent);
                    } else
                    {
                        messageHandler("Message without payload");
                    }
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
