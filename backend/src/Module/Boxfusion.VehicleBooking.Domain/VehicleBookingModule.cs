using System.Reflection;
using Abp.AspNetCore.Configuration;
using Abp.AutoMapper;
using Abp.Modules;
using Boxfusion.VehicleBooking.Domain.Authorization;
using Intent.RoslynWeaver.Attributes;
using Shesha;
using Shesha.Modules;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("Boxfusion.Modules.Domain.Module", Version = "1.0")]

namespace Boxfusion.VehicleBooking.Domain
{
    [IntentManaged(Mode.Ignore)]
    /// <summary>
    /// VehicleBookingCommon Module
    /// </summary>
    [DependsOn(
        typeof(SheshaCoreModule),
        typeof(SheshaApplicationModule)
    )]
    public class VehicleBookingModule : SheshaModule
    {
        public override SheshaModuleInfo ModuleInfo => new SheshaModuleInfo("Boxfusion.VehicleBooking")
        {
            FriendlyName = "VehicleBooking",
            Publisher = "Boxfusion",
        };
        /// inheritedDoc
        public override void Initialize()
        {
            var thisAssembly = Assembly.GetExecutingAssembly();
            IocManager.RegisterAssemblyByConvention(thisAssembly);

            Configuration.Modules.AbpAutoMapper().Configurators.Add(
                // Scan the assembly for classes which inherit from AutoMapper.Profile
                cfg => cfg.AddMaps(thisAssembly)
            );
        }

        /// inheritedDoc
        public override void PreInitialize()
        {
            base.PreInitialize();
            Configuration.Authorization.Providers.Add<VehicleBookingAuthorizationProvider>();
        }

        /// inheritedDoc
        public override void PostInitialize()
        {
            Configuration.Modules.AbpAspNetCore().CreateControllersForAppServices(
                typeof(VehicleBookingModule).Assembly,
                moduleName: "VehicleBookingCommon",
                useConventionalHttpVerbs: true);
        }
    }
}
