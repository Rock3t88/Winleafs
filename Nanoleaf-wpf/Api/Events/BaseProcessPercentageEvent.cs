﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Media;
using Winleafs.Api;
using Winleafs.Api.Endpoints.Interfaces;
using Winleafs.Models.Enums;
using Winleafs.Models.Models.Layouts;
using Winleafs.Wpf.Helpers;

namespace Winleafs.Wpf.Api.Events
{
    /// <summary>
    /// Base class for process percentage events
    /// </summary>
    public abstract class BaseProcessPercentageEvent : IEvent
    {
        private Timer _processCheckTimer;
        private Timer _effectTimer;
        private Orchestrator _orchestrator;
        private string _processName;
        private IExternalControlEndpoint _externalControlEndpoint;
        private PercentageProfile _percentageProfile;
        private float _percentagePerStep;
        private int _amountOfSteps;
        private SolidColorBrush _whiteColor = Brushes.White;
        private SolidColorBrush _redColor = Brushes.DarkRed;

        public BaseProcessPercentageEvent(Orchestrator orchestrator, string processName)
        {
            _orchestrator = orchestrator;
            _processName = processName;

            var client = NanoleafClient.GetClientForDevice(_orchestrator.Device);
            _externalControlEndpoint = client.ExternalControlEndpoint;

            //Check if the user has somehow messed up their percentage profile, then we create a single step percentage profile
            if (_orchestrator.Device.PercentageProfile == null || _orchestrator.Device.PercentageProfile.Steps.Count == 0)
            {
                _percentageProfile = new PercentageProfile();
                var step = new PercentageStep();

                foreach (var panel in client.LayoutEndpoint.GetLayout().PanelPositions)
                {
                    step.PanelIds.Add(panel.PanelId);
                }

                _percentageProfile.Steps.Add(step);
                _percentagePerStep = 100f;
                _amountOfSteps = 1;
            }
            else
            {
                _percentageProfile = _orchestrator.Device.PercentageProfile;
                _amountOfSteps = _percentageProfile.Steps.Count;
                _percentagePerStep = 100f / _amountOfSteps;
            }            

            _processCheckTimer = new Timer(10000); //TODO: increase
            _processCheckTimer.Elapsed += CheckProcess;
            _processCheckTimer.AutoReset = true;
            _processCheckTimer.Start();

            _effectTimer = new Timer(1000); //TODO: tune
            _effectTimer.Elapsed += ApplyEffect;
            _effectTimer.AutoReset = true;
        }

        private void CheckProcess(object source, ElapsedEventArgs e)
        {
            Task.Run(() => CheckProcessAsync());
        }

        private async Task CheckProcessAsync()
        {
            //Check here if a process is running when the timer is not yet running, then execute TryStartEffect()
            if (!_effectTimer.Enabled)
            {
                Process[] processes = Process.GetProcessesByName(_processName);

                if (processes.Length > 0)
                {
                    await TryStartEffect();
                }
            }            
        }

        private async Task TryStartEffect()
        {
            if (await _orchestrator.TrySetOperationMode(OperationMode.Event))
            {
                await _externalControlEndpoint.PrepareForExternalControl();
                _effectTimer.Start();
            }
        }

        private void ApplyEffect(object source, ElapsedEventArgs e)
        {
            try
            {
                using (var memoryReader = new MemoryReader(_processName))
                {
                    Task.Run(() => ApplyEffectLocalAsync(memoryReader));
                }
            }
            catch
            {
                //Stop the event if the process does not exist anymore
                StopEvent();

                //Let orchestrator know that the process event has stopped so it can continue with normal program, will not fail since an event can only be activated when no override is active
                //Always return to schedule since only 1 event can be active at a time
                Task.Run(() => _orchestrator.TrySetOperationMode(OperationMode.Schedule));
            }
        }

        protected abstract Task ApplyEffectLocalAsync(MemoryReader memoryReader);

        protected async Task ApplyPercentageEffect(float percentage)
        {
            var numberOfActiveSteps = _amountOfSteps; //Default the percentage is deemed 100
            if (!float.IsNaN(percentage))
            {
                Math.Max(0, (int)Math.Floor(percentage / _percentagePerStep));
            }            
            
            var activeSteps = _percentageProfile.Steps.Take(numberOfActiveSteps);
            var inactiveSteps = _percentageProfile.Steps.Except(activeSteps);

            foreach (var step in activeSteps)
            {
                foreach (var panel in step.PanelIds)
                {
                    await _externalControlEndpoint.SetPanelColorAsync(panel, _redColor.Color.R, _redColor.Color.G, _redColor.Color.B);
                }
            }

            foreach (var step in inactiveSteps)
            {
                foreach (var panel in step.PanelIds)
                {
                    await _externalControlEndpoint.SetPanelColorAsync(panel, _whiteColor.Color.R, _whiteColor.Color.G, _whiteColor.Color.B);
                }
            }
        }

        public void StopEvent()
        {
            _effectTimer.Stop();
        }

        public bool IsActive()
        {
            return _effectTimer.Enabled;
        }

        public abstract string GetDescription();

        public int GetBrightness()
        {
            return -1;
        }
    }
}
