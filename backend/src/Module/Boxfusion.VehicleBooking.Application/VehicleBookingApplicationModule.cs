using System.Reflection;
using System.Threading.Tasks;
using Abp.AspNetCore;
using Abp.AspNetCore.Configuration;
using Abp.AutoMapper;
using Abp.Modules;
using Abp.Reflection.Extensions;
using Intent.RoslynWeaver.Attributes;
using Boxfusion.VehicleBooking.Domain;
using Shesha;
using Shesha.Modules;
using Shesha.Web.FormsDesigner;

[assembly: IntentTemplate("Boxfusion.Modules.Application.Services.AppService", Version = "1.0")]
[assembly: DefaultIntentManaged(Mode.Fully)]

namespace Boxfusion.VehicleBooking.Application
{
    [IntentManaged(Mode.Ignore)]
    /// <summary>
    /// VehicleBooking Module
    /// </summary>
    [DependsOn(
        typeof(VehicleBookingModule),
        typeof(SheshaCoreModule),
        typeof(AbpAspNetCoreModule)
    )]
    public class VehicleBookingApplicationModule : SheshaSubModule<VehicleBookingModule>
    {
        public override async Task<bool> InitializeConfigurationAsync()
        {
            // Import any configuration embeded as resources in this assembly on application start-up.
            return await ImportConfigurationAsync();
        }

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

            Configuration.Modules.AbpAspNetCore()
                .CreateControllersForAppServices(
                    typeof(SheshaCoreModule).GetAssembly()
                );

            Configuration.Modules.AbpAspNetCore()
                 .CreateControllersForAppServices(
                     typeof(SheshaApplicationModule).GetAssembly()
                 );

            Configuration.Modules.AbpAspNetCore()
                 .CreateControllersForAppServices(
                     typeof(SheshaFormsDesignerModule).GetAssembly()
                 );

            Configuration.Modules.AbpAspNetCore()
                 .CreateControllersForAppServices(
                     typeof(SheshaFrameworkModule).GetAssembly()
                 );

            Configuration.Modules.AbpAspNetCore().CreateControllersForAppServices(
               typeof(VehicleBookingApplicationModule).Assembly,
               moduleName: "VehicleBooking",
                useConventionalHttpVerbs: true);

            Configuration.Modules.AbpAspNetCore()
                 .CreateControllersForAppServices(
                     typeof(VehicleBookingModule).GetAssembly()
                 );

            Configuration.Modules.AbpAspNetCore()
                 .CreateControllersForAppServices(
                     typeof(VehicleBookingApplicationModule).GetAssembly()
                 );
        }
    }
}
