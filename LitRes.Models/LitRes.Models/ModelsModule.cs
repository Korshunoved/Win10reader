using System.Linq;

using Autofac;

using LitRes.Services.Connectivity;

namespace LitRes
{
	public class ModelsModule : Module
	{
		protected override void Load( ContainerBuilder builder )
		{
			base.Load( builder );

			builder.RegisterType<RestClientService>().As<IRestClientService>();
			builder.RegisterType<SessionlessConnection>().As<ISessionlessConnection>();
			builder.RegisterType<SessionEstablisherService>().As<ISessionEstablisherService>().SingleInstance();
			builder.RegisterType<SessionAwareConnection>().As<ISessionAwareConnection>();
			builder.RegisterType<CatalitClient>().As<ICatalitClient>();
		}
	}
}
