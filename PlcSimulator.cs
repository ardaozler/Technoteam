using System.Diagnostics;

namespace Technoteam
{
    public enum PlcState
    {
        Normal, //motor running normally
        EmergencyStop, //motor mechanically stopped due to emergency set the actual speed to 0
        MaintenanceNeeded //motor stopped due to overheating, needs maintenance, slow the actual speed to 0
    }
    public class PlcSimulator
    {
        public int Temperature { get; private set; } = 25;
        public int ActualSpeed { get; private set; } = 0;
        public PlcState State { get; private set; } = PlcState.Normal;

        public int TargetSpeed { get; set; } = 0;
        public bool ManualEmergencyStopRequested { get; set; } = false;
        public bool ManualStartRequested { get; set; } = false;

        public const int TemperatureLimit = 105;

        private const int MaxSpeed = 2000;
        private double _timeAboveTempLimit = 0;
        private Random _rand = new Random(10); //TODO: remove the seed
        private const int SpeedChangeRate = 100; //RPM per second
        private const int EmergencyDecelerationRate = 500; //RPM per second
        private const int AmbientTemperature = 25;
        private int heatingThresholdRPM; //different for each plc, just random for simulation
        private int _operatingTemperature;

        public bool IsOverheatWarning => State == PlcState.Normal && Temperature > TemperatureLimit;

        public void Start()
        {
            heatingThresholdRPM = _rand.Next(1200, 1900);
            _operatingTemperature = _rand.Next(70, 90);
        }

        public void Update(double tDelta)
        {
            if (ManualEmergencyStopRequested)
            {
                EmergencyStopRequested();
                ManualEmergencyStopRequested = false;
            }

            UpdateTemperature();
            StateUpdate(tDelta);
            UpdateSpeed(tDelta);
        }

        private void UpdateSpeed(double tDelta)
        {
            if (State == PlcState.EmergencyStop) //mechanical stop
            {
                ActualSpeed = 0;
                return;
            }

            var rate = State == PlcState.MaintenanceNeeded //cut power bc overheat
                ? EmergencyDecelerationRate
                : SpeedChangeRate;

            var speedDelta = rate * tDelta;

            if (ActualSpeed < TargetSpeed)
            {
                ActualSpeed += (int)speedDelta;
                if (ActualSpeed > TargetSpeed)
                {
                    ActualSpeed = TargetSpeed;
                }
            }
            else if (ActualSpeed > TargetSpeed)
            {
                ActualSpeed -= (int)speedDelta;
                if (ActualSpeed < TargetSpeed)
                {
                    ActualSpeed = TargetSpeed;
                }
            }
        }

        private void UpdateTemperature()
        {
            if (ActualSpeed > 0)
            {

                if (Temperature < _operatingTemperature - 2 || ActualSpeed >= heatingThresholdRPM)
                {
                    Temperature += _rand.Next(0, 3);
                }
                else if (Temperature > _operatingTemperature + 2)
                {
                    Temperature -= _rand.Next(1, 3);
                }
                else
                {
                    Temperature += _rand.Next(-1, 2);
                }
            }
            else
            {
                if (Temperature > AmbientTemperature)
                {
                    Temperature -= _rand.Next(0, 2);
                    if (Temperature < AmbientTemperature)
                    {
                        Temperature = AmbientTemperature;
                    }
                }
                else if (Temperature < AmbientTemperature)
                {
                    Temperature++;
                }
            }
        }

        private void StateUpdate(double tDelta)
        {
            switch (State)
            {
                case PlcState.Normal:
                    NormalStateUpdate(tDelta);
                    break;
                case PlcState.EmergencyStop:
                    EmergencyStopStateUpdate();
                    break;
                case PlcState.MaintenanceNeeded:
                    MaintenanceStateUpdate();
                    break;
                default:
                    break;
            }
        }

        private void NormalStateUpdate(double tDelta)
        {
            if (Temperature > TemperatureLimit)
            {
                _timeAboveTempLimit += tDelta;
                if (_timeAboveTempLimit >= 10)
                {
                    State = PlcState.MaintenanceNeeded;
                    _timeAboveTempLimit = 0;
                }
            }
            else
            {
                _timeAboveTempLimit = 0;
            }
        }

        private void EmergencyStopStateUpdate()
        {
            TargetSpeed = 0;
            if (ManualStartRequested && Temperature < TemperatureLimit)
            {
                State = PlcState.Normal;
                ManualStartRequested = false;
            }
        }

        private void MaintenanceStateUpdate() //no way to recover from this state in the simulation
        {
            TargetSpeed = 0;
        }

        private void EmergencyStopRequested()
        {
            if (State == PlcState.MaintenanceNeeded) return;
            Debug.WriteLine("Emergency stop requested!");
            State = PlcState.EmergencyStop;
        }
    }
}
