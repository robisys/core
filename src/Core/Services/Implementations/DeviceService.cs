﻿using System;
using System.Threading.Tasks;
using Bit.Core.Models.Table;
using Bit.Core.Repositories;

namespace Bit.Core.Services
{
    public class DeviceService : IDeviceService
    {
        private readonly IDeviceRepository _deviceRepository;
        private readonly IPushRegistrationService _pushRegistrationService;

        public DeviceService(
            IDeviceRepository deviceRepository,
            IPushRegistrationService pushRegistrationService)
        {
            _deviceRepository = deviceRepository;
            _pushRegistrationService = pushRegistrationService;
        }

        public async Task SaveAsync(Device device)
        {
            if(device.Id == default(Guid))
            {
                await _deviceRepository.CreateAsync(device);
            }
            else
            {
                device.RevisionDate = DateTime.UtcNow;
                await _deviceRepository.ReplaceAsync(device);
            }

            await _pushRegistrationService.CreateOrUpdateRegistrationAsync(device);
        }

        public async Task ClearTokenAsync(Device device)
        {
            await _deviceRepository.ClearPushTokenByIdentifierAsync(device.Identifier);
            await _pushRegistrationService.DeleteRegistrationAsync(device.Id);
        }

        public async Task DeleteAsync(Device device)
        {
            await _deviceRepository.DeleteAsync(device);
            await _pushRegistrationService.DeleteRegistrationAsync(device.Id);
        }
    }
}
