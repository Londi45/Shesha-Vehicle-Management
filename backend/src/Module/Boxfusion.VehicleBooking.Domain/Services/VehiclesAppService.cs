using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.UI;
using Boxfusion.VehicleBooking.Domain.Domain;
using Boxfusion.VehicleBooking.Domain.Domain.Enums;
using Boxfusion.VehicleBooking.Domain.Services.Dtos;
using Microsoft.AspNetCore.Mvc;
using NHibernate.Linq;
using Shesha;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Boxfusion.VehicleBooking.Domain.Services
{
    [AbpAuthorize]
    public class VehiclesAppService : SheshaAppServiceBase
    {
        private readonly IRepository<Booking, Guid> _bookingRepository;
        private readonly IRepository<Vehicle, Guid> _vehicleRepository;

        public VehiclesAppService(
            IRepository<Booking, Guid> bookingRepository,
            IRepository<Vehicle, Guid> vehicleRepository)
        {
            _bookingRepository = bookingRepository;
            _vehicleRepository = vehicleRepository;
        }

        [HttpPost]
        public async Task<List<AvailableVehiclesDTOs>> postAvailalableVehicles(GetAvailableVehicles input)
        {
            
            var (hasDates, start, end) = BookingRules.ResolveSearchPeriod(input.StartDate, input.EndDate, DateTime.Now);

           
            var bookedVehicleIds = _bookingRepository.GetAll()
                .Where(BookingRules.ActiveOverlap(start, end))
                .Select(b => b.Vehicle.Id);

            var query = _vehicleRepository.GetAll()
                .Where(v => v.Status == RefListVehicleStatuses.Active)
                .Where(v => !bookedVehicleIds.Contains(v.Id));

            if (input.BranchId.HasValue)
            {
                var branchId = input.BranchId;
                query = query.Where(v => v.Branch.Id == branchId);
            }

            if (input.VehicleType.HasValue)
            {
                var vehicleType = input.VehicleType.Value;
                query = query.Where(v => v.VehicleType == vehicleType);
            }

            var vehicles = await query
                .OrderBy(v => v.DailyRate)
                .ToListAsync();

            return vehicles.Select(v => new AvailableVehiclesDTOs
            {
                Id = v.Id,
                RegistrationNumber = v.RegistrationNumber,
                Make = v.Make,
                Model = v.Model,
                Year = v.Year,
                VehicleType = v.VehicleType,
                BranchName = v.Branch?.Name,
                
                EstimatedTotal = BookingRules.EstimateTotal(v.DailyRate, hasDates, start, end)
            }).ToList();
        }

        [HttpGet]
        public async Task<PagedResultDto<AvailableVehiclesDTOs>> GetAvailableVehiclesPaged([FromQuery] GetAvailableVehicles input)
        {
            
            var list = await postAvailalableVehicles(input);
            return new PagedResultDto<AvailableVehiclesDTOs>(list.Count, list);
        }
    }
}