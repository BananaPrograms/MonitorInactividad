using NAudio.CoreAudioApi;

namespace MonitorInactividad
{
    public class AudioDetector
    {
        private readonly MMDevice device;

        public AudioDetector()
        {
            var enumerator = new MMDeviceEnumerator();
            device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
        }

        public bool HayAudioActivo(float umbral = 0.01f)
        {
            // MasterPeakValue retorna 0.0 a 1.0
            return device.AudioMeterInformation.MasterPeakValue > umbral;
        }
    }
}
