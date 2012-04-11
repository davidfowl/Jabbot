using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JabbR.Client;
using Jabbot.Core;
using System.ComponentModel.Composition.Primitives;
using System.ComponentModel.Composition.Hosting;
using JabbR.Client.Models;
using Jabbot.Sprockets.Core;
using System.IO;
using System.Web.Hosting;
using System.Diagnostics;
using System.Threading.Tasks;
using Jabbot.Models;
using System.Reflection;

namespace Jabbot
{
    public class Bot : IBot
    {

        private const string ExtensionsFolder = "Sprockets";

        private readonly string _password = string.Empty;
        private readonly string _url = string.Empty;
        private JabbRClient _client;

        private readonly List<ISprocket> _sprockets = new List<ISprocket>();
        private readonly List<IUnhandledMessageSprocket> _unhandledMessageSprockets = new List<IUnhandledMessageSprocket>();
        private readonly HashSet<string> _rooms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        internal List<ISprocket> Sprockets
        {
            get
            {
                return _sprockets;
            }
        }

        private ComposablePartCatalog _catalog = null;
        private CompositionContainer _container = null;

        private bool _isActive = false;
        private bool _containerInitialized = false;

        public Bot(string url, string name, string password)
        {
            Name = name;
            _url = url;
            _password = password;

            InitializeClient();
            CreateCompositionContainer();
            InitializeContainer();
        }

        private void InitializeClient()
        {
            _client = new JabbRClient(_url);

            _client.MessageReceived += (message, room) =>
            {
                ProcessMessage(message, room);
            };
        }

        public string Name { get; private set; }

        public System.Net.ICredentials Credentials
        {
            get { throw new NotImplementedException(); }
        }

        public event Action Disconnected
        {
            add
            {
                _client.Disconnected += value;
            }
            remove
            {
                _client.Disconnected -= value;
            }
        }

        public event Action<Message, string> MessageReceived;

        public void StartUp()
        {
            _client.Connect(Name, _password).ContinueWith(task =>
            {
                if (!task.IsFaulted)
                {
                    _client.JoinRoom("twitterbot");
                }
                else
                {
                    Console.WriteLine(task.Exception);
                }
            }).Wait();
        }

        public void ShutDown()
        {
            _client.Disconnect();
        }

        public void JoinRoom(string room)
        {
            _client.JoinRoom(room);
        }

        public void CreateRoom(string room)
        {
            _client.CreateRoom(room);
        }

        public void PrivateReply(string toName, string message)
        {
            _client.SendPrivateMessage(toName, message);
        }

        public void Send(string message, string room)
        {
            _client.Send(message, room);
        }


        private void ProcessMessage(JabbR.Client.Models.Message message, string room)
        {
            Console.WriteLine("{0} {1} {2}", room, message.Content, message.User.Name);
            if (message.User.Name != Name)
            {
                Send("Received " + message.Content + " from " + message.User.Name + " in " + room, room);
            }
            Task.Factory.StartNew(() =>
            {
                string content = message.Content;
                string name = message.User.Name;

                // Ignore replies from self
                if (name.Equals(Name, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
                if (MessageReceived != null)
                {
                    MessageReceived(message, room);
                }

                ChatMessage chatMessage = new ChatMessage(message.Content, message.User.Name, room);

                bool handled = false;

                handled = HandleMessageWithSprockets(chatMessage, handled);

                if (!handled)
                {
                    ProcessUnhandledMessage(chatMessage);
                }
            })
            .ContinueWith(task =>
            {
                // Just write to debug output if it failed
                if (task.IsFaulted)
                {
                    Debug.WriteLine("JABBOT: Failed to process messages. {0}", task.Exception.GetBaseException());
                    Send("JABBOT: Failed to process messages:" + task.Exception.GetBaseException().ToString(), room);
                }
            });
        }

        private void ProcessUnhandledMessage(ChatMessage chatMessage)
        {
            // Loop over the unhandled message sprockets
            foreach (var handler in _unhandledMessageSprockets)
            {
                // Stop at the first one that handled the message
                if (handler.Handle(chatMessage, this))
                {
                    break;
                }
            }
        }

        private bool HandleMessageWithSprockets(ChatMessage chatMessage, bool handled)
        {
            // Loop over the registered sprockets
            foreach (var handler in _sprockets)
            {
                // Stop at the first one that handled the message
                if (handler.Handle(chatMessage, this))
                {
                    handled = true;
                    break;
                }
            }
            return handled;
        }

        private CompositionContainer CreateCompositionContainer()
        {
            if (_container == null)
            {
                string extensionsPath = GetExtensionsPath();

                // If the extensions folder exists then use them
                if (Directory.Exists(extensionsPath))
                {
                    _catalog = new AggregateCatalog(
                                new AssemblyCatalog(typeof(Bot).Assembly),
                                new DirectoryCatalog(extensionsPath, "*.dll"));
                }
                else
                {
                    _catalog = new AssemblyCatalog(typeof(Bot).Assembly);
                }

                _container = new CompositionContainer(_catalog);
            }
            return _container;
        }

        private void InitializeContainer()
        {
            if (!_containerInitialized)
            {
                try
                {
                    var container = CreateCompositionContainer();
                    // Add all the sprockets to the sprocket list
                    foreach (var sprocket in container.GetExportedValues<ISprocket>())
                    {
                        AddSprocket(sprocket);
                    }
                    // Add all the sprockets to the sprocket list
                    foreach (var sprocket in container.GetExportedValues<IUnhandledMessageSprocket>())
                    {
                        AddUnhandledMessageSprocket(sprocket);
                    }
                    _containerInitialized = true;
                }
                catch (ReflectionTypeLoadException ex)
                {
                    throw ex.LoaderExceptions.First();
                }
                catch (Exception e)
                {
                    throw e.GetBaseException();
                }

            }
            else
            {
                throw new InvalidOperationException("Container already initialized");
            }
        }

        /// <summary>
        /// Run after connection to give sprockets a chance to perform any startup actions
        /// </summary>
        private void IntializeSprockets()
        {
            var container = CreateCompositionContainer();
            // Run all sprocket initializers
            foreach (var sprocketInitializer in container.GetExportedValues<ISprocketInitializer>())
            {
                try
                {
                    sprocketInitializer.Initialize(this);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(String.Format("Unable to Initialize {0}:{1}", sprocketInitializer.GetType().Name, ex.GetBaseException().Message));
                }
            }
        }



        /// <summary>
        /// Add a sprocket to the bot instance
        /// </summary>
        public void AddSprocket(ISprocket sprocket)
        {
            _sprockets.Add(sprocket);
        }

        /// <summary>
        /// Remove a sprocket from the bot instance
        /// </summary>
        public void RemoveSprocket(ISprocket sprocket)
        {
            _sprockets.Remove(sprocket);
        }

        /// <summary>
        /// Add a sprocket to the bot instance
        /// </summary>
        public void AddUnhandledMessageSprocket(IUnhandledMessageSprocket sprocket)
        {
            _unhandledMessageSprockets.Add(sprocket);
        }

        /// <summary>
        /// Remove a sprocket from the bot instance
        /// </summary>
        public void RemoveUnhandledMessageSprocket(IUnhandledMessageSprocket sprocket)
        {
            _unhandledMessageSprockets.Remove(sprocket);
        }

        /// <summary>
        /// Remove all sprockets
        /// </summary>
        public void ClearSprockets()
        {
            _sprockets.Clear();
        }


        private static string GetExtensionsPath()
        {
            string rootPath = null;
            if (HostingEnvironment.IsHosted)
            {

                rootPath = HostingEnvironment.ApplicationPhysicalPath;
            }
            else
            {
                rootPath = Directory.GetCurrentDirectory();
            }

            return Path.Combine(rootPath, ExtensionsFolder);
        }


    }
}
