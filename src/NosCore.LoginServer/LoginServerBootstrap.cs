﻿//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// 
// Copyright (C) 2019 - NosCore
// 
// NosCore is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using AutofacSerilogIntegration;
using NosCore.Packets;
using NosCore.Packets.Interfaces;
using DotNetty.Buffers;
using DotNetty.Codecs;
using FastExpressionCompiler;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Threading;
using NosCore.Core;
using NosCore.Core.Encryption;
using NosCore.Core.HttpClients.AuthHttpClients;
using NosCore.Core.HttpClients.ChannelHttpClients;
using NosCore.Core.HttpClients.ConnectedAccountHttpClients;
using NosCore.Core.I18N;
using NosCore.Dao.Interfaces;
using NosCore.Data.Dto;
using NosCore.Data.Enumerations;
using NosCore.Data.Enumerations.I18N;
using NosCore.Database;
using NosCore.Database.Entities;
using NosCore.GameObject;
using NosCore.GameObject.ComponentEntities.Interfaces;
using NosCore.GameObject.HttpClients.BlacklistHttpClient;
using NosCore.GameObject.Networking;
using NosCore.GameObject.Networking.ClientSession;
using NosCore.GameObject.Networking.LoginService;
using NosCore.PacketHandlers.Login;
using Serilog;
using ILogger = Serilog.ILogger;
using NosCore.Dao;
using NosCore.GameObject.Configuration;
using NosCore.Shared.Configuration;
using NosCore.Shared.I18N;

namespace NosCore.LoginServer
{
    public static class LoginServerBootstrap
    {
        private const string ConfigurationPath = "../../../configuration";
        private const string Title = "NosCore - LoginServer";
        private const string ConsoleText = "LOGIN SERVER - NosCoreIO";
        private static readonly ILogger _logger = Logger.GetLoggerConfiguration().CreateLogger();

        private static readonly LoginConfiguration _loginConfiguration = new LoginConfiguration();
        private static DataAccessHelper _dataAccess = null!;

        private static void InitializeConfiguration()
        {
            new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory() + ConfigurationPath)
                .AddYamlFile("login.yml", false)
                .Build()
                .Bind(_loginConfiguration);

            Validator.ValidateObject(_loginConfiguration, new ValidationContext(_loginConfiguration),
                true);

            var optionsBuilder = new DbContextOptionsBuilder<NosCoreContext>();
            optionsBuilder.UseNpgsql(_loginConfiguration.Database!.ConnectionString);
            _dataAccess = new DataAccessHelper();
            _dataAccess.Initialize(optionsBuilder.Options, _logger);

            LogLanguage.Language = _loginConfiguration.Language;
        }

        private static void InitializeContainer(ContainerBuilder containerBuilder)
        {
            containerBuilder.Register<IDbContextBuilder>(c => _dataAccess).AsImplementedInterfaces().SingleInstance();
            containerBuilder.RegisterLogger();
            containerBuilder.RegisterInstance(_loginConfiguration!).As<LoginConfiguration>().As<ServerConfiguration>();
            containerBuilder.RegisterType<Dao<Account, AccountDto, long>>().As<IDao<AccountDto, long>>()
                .SingleInstance();
            containerBuilder.RegisterType<LoginDecoder>().As<MessageToMessageDecoder<IByteBuffer>>();
            containerBuilder.RegisterType<LoginEncoder>().As<MessageToMessageEncoder<IEnumerable<IPacket>>>();
            containerBuilder.RegisterType<LoginServer>().PropertiesAutowired();
            containerBuilder.RegisterType<ClientSession>();

            containerBuilder.RegisterType<NetworkManager>();
            containerBuilder.RegisterType<PipelineFactory>();
            containerBuilder.RegisterType<LoginService>().AsImplementedInterfaces();
            containerBuilder.RegisterType<AuthHttpClient>().AsImplementedInterfaces();
            containerBuilder.RegisterType<ChannelHttpClient>().SingleInstance().AsImplementedInterfaces();
            containerBuilder.RegisterType<ConnectedAccountHttpClient>().AsImplementedInterfaces();
            containerBuilder.RegisterAssemblyTypes(typeof(BlacklistHttpClient).Assembly)
                .Where(t => t.Name.EndsWith("HttpClient"))
                .AsImplementedInterfaces()
                .PropertiesAutowired();
            containerBuilder.Register(c => new Channel
            {
                MasterCommunication = _loginConfiguration!.MasterCommunication,
                ClientType = ServerType.LoginServer,
                ClientName = $"{ServerType.LoginServer}",
                Port = _loginConfiguration.Port,
                Host = _loginConfiguration.Host!
            });
            foreach (var type in typeof(NoS0575PacketHandler).Assembly.GetTypes())
            {
                if (typeof(IPacketHandler).IsAssignableFrom(type) && typeof(ILoginPacketHandler).IsAssignableFrom(type))
                {
                    containerBuilder.RegisterType(type)
                        .AsImplementedInterfaces()
                        .PropertiesAutowired();
                }
            }

            var listofpacket = typeof(IPacket).Assembly.GetTypes()
                .Where(p => ((p.Namespace == "NosCore.Packets.ServerPackets.Login") ||
                        (p.Namespace == "NosCore.Packets.ClientPackets.Login"))
                    && p.GetInterfaces().Contains(typeof(IPacket)) && p.IsClass && !p.IsAbstract).ToList();
            containerBuilder.Register(c => new Deserializer(listofpacket))
                .AsImplementedInterfaces()
                .SingleInstance();
            containerBuilder.Register(c => new Serializer(listofpacket))
                .AsImplementedInterfaces()
                .SingleInstance();
        }

        public static async Task Main()
        {
            try
            {
                await BuildHost(new string[0]).RunAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, LogLanguage.Instance.GetMessageFromKey(LogLanguageKey.EXCEPTION), ex.Message);
            }
        }

        private static IHost BuildHost(string[] _)
        {
            return new HostBuilder()
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddSerilog();
                })
                .UseConsoleLifetime()
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureServices((hostContext, services) =>
                {
                    try { Console.Title = Title; } catch (PlatformNotSupportedException) { }
                    Logger.Initialize(new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory() + ConfigurationPath)
                        .AddYamlFile("logger.yml", false)
                        .Build());
                    Logger.PrintHeader(ConsoleText);
                    InitializeConfiguration();

                    services.AddLogging(builder => builder.AddFilter("Microsoft", LogLevel.Warning));
                    services.AddHttpClient();
                    services.RemoveAll<IHttpMessageHandlerBuilderFilter>();
                    services.Configure<ConsoleLifetimeOptions>(o => o.SuppressStatusMessages = true);
                    var containerBuilder = new ContainerBuilder();
                    InitializeContainer(containerBuilder);
                    containerBuilder.Populate(services);
                    var container = containerBuilder.Build();

                    Task.Run(container.Resolve<LoginServer>().RunAsync).Forget();
                    TypeAdapterConfig.GlobalSettings.ForDestinationType<IInitializable>()
                       .AfterMapping(dest => Task.Run(dest.InitializeAsync));
                    TypeAdapterConfig.GlobalSettings.EnableJsonMapping();
                    TypeAdapterConfig.GlobalSettings.Compiler = exp => exp.CompileFast();
                })
                .Build();
        }
    }
}