//using Durandal.Common.Audio.FFT;
//using Durandal.Common.Utils.IO;
//using NAudio.Wave;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace NativeGL.Audio
//{
//    public class SpectrographSampleProvider : ISampleProvider
//    {
//        private const int SPECTRUM_WIDTH = 1024;
//        private ISampleProvider _innerProvider;
//        private BasicBuffer<float> _waveBuffer = new BasicBuffer<float>(SPECTRUM_WIDTH * 8);
//        private float[] _lastSpectrograph = new float[SPECTRUM_WIDTH / 2];
//        private float[] _lastWaveform = new float[SPECTRUM_WIDTH];

//        public SpectrographSampleProvider(ISampleProvider innerProvider)
//        {
//            _innerProvider = innerProvider;
//        }

//        public WaveFormat WaveFormat
//        {
//            get
//            {
//                return _innerProvider.WaveFormat;
//            }
//        }

//        public float[] GetSpectrograph()
//        {
//            // Calculate the spectrograph of the remaining buffered waveform
//            if (_waveBuffer.Available > SPECTRUM_WIDTH)
//            {
//                _lastWaveform = _waveBuffer.Read(SPECTRUM_WIDTH);
//                ComplexF[] complex = new ComplexF[SPECTRUM_WIDTH];
//                for (int c = 0; c < SPECTRUM_WIDTH; c++)
//                {
//                    complex[c] = new ComplexF(_lastWaveform[c], 0);
//                }

//                Fourier.FFT(complex, SPECTRUM_WIDTH, FourierDirection.Forward);
//                for (int c = 0; c < _lastSpectrograph.Length; c++)
//                {
//                    _lastSpectrograph[c] = complex[c].GetModulus();
//                }
//            }

//            return _lastSpectrograph;
//        }

//        public float[] GetWaveform()
//        {
//            return _lastWaveform;
//        }

//        public int Read(float[] buffer, int offset, int count)
//        {
//            float[] buf = new float[count];
//            int returnVal = _innerProvider.Read(buf, 0, count);
//            _waveBuffer.Write(buf);
//            Array.Copy(buf, 0, buffer, offset, count);
//            return returnVal;
//        }
//    }
//}
