using NAudio.CoreAudioApi;
using NAudio.Dsp;
using NAudio.Flac;
using NAudio.Wave;
using NAudio.WindowsMediaFormat;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Media;
using WpfLib;

namespace AudioLib
{
    /// NAudioとNAudioFlacはNuGetで追加インストールする
    /// 参考 : NAudio C# プログラミング解説
    ///         https://so-zou.jp/software/tech/programming/c-sharp/media/audio/naudio/
    ///     C#で音声波形を表示する音楽プレーヤーを作る
    ///         https://qiita.com/lenna_kun/items/a0f03447bb893c9ab937
    ///         
    ///     参照の追加(アセンブリ)
    ///     Windows.Media : PresentationCore, WindowsBaseを追加
    ///     Complex : System.Numericsを追加
    public class AudioLib
    {
        //  エンコーディングフォーマット
        private Dictionary<WaveFormatEncoding, string> mEncording =
            new Dictionary<WaveFormatEncoding, string>() {
                {WaveFormatEncoding.Unknown, "UNKNOWN" },
                {WaveFormatEncoding.Pcm, "PCM" },
                {WaveFormatEncoding.Adpcm, "ADPCM" },
                {WaveFormatEncoding.IeeeFloat, "IEEE FLOAT" },
                {WaveFormatEncoding.Vselp, "VSELP" },
                {WaveFormatEncoding.IbmCvsd, "IBM CVSD" },
                {WaveFormatEncoding.ALaw, "ALAW" },
                {WaveFormatEncoding.MuLaw, "MULAW" },
                {WaveFormatEncoding.Dts, "DTS" },
                {WaveFormatEncoding.Drm, "DRM" },
                {WaveFormatEncoding.WmaVoice9, "WMAVOICE9" },
                {WaveFormatEncoding.OkiAdpcm, "OKI ADPCM" },
                {WaveFormatEncoding.DviAdpcm, "DVI/IMA ADPCM" },
                {WaveFormatEncoding.MediaspaceAdpcm, "MEDIASPACE ADPCM" },
                {WaveFormatEncoding.SierraAdpcm, "SIERRA ADPCM" },
                {WaveFormatEncoding.G723Adpcm, "G723 ADPCM" },
                {WaveFormatEncoding.DigiStd, "DIGISTAD" },
                {WaveFormatEncoding.DigiFix, "DIGIFIX" },
                {WaveFormatEncoding.DialogicOkiAdpcm, "DIALOGIC OKI ADPCM" },
                {WaveFormatEncoding.MediaVisionAdpcm, "MEDIAVISION ADPCM" },
                {WaveFormatEncoding.CUCodec, "CU CODEC" },
                {WaveFormatEncoding.YamahaAdpcm, "YAMAHA ADPCM" },
                {WaveFormatEncoding.SonarC, "SONRAC" },
                {WaveFormatEncoding.DspGroupTrueSpeech, "DSPGROUP TRUESPEEH" },
                {WaveFormatEncoding.EchoSpeechCorporation1, "ECHOSC1" },
                {WaveFormatEncoding.AudioFileAf36, "AUDIOFILE AF36" },
                {WaveFormatEncoding.Aptx, "APTX" },
                {WaveFormatEncoding.AudioFileAf10, "AUDIOFILE AF10" },
                {WaveFormatEncoding.Prosody1612, "PROSODY 1612" },
                {WaveFormatEncoding.Lrc, "LRC" },
                {WaveFormatEncoding.DolbyAc2, "DOLBY AC2" },
                {WaveFormatEncoding.Gsm610, "GSM610" },
                {WaveFormatEncoding.MsnAudio, "MSNAUDIO" },
                {WaveFormatEncoding.AntexAdpcme, "ANTEX ADPCME" },
                {WaveFormatEncoding.ControlResVqlpc, "CONTROL RES VQLPC" },
                {WaveFormatEncoding.DigiReal, "DIGIREAL" },
                {WaveFormatEncoding.DigiAdpcm, "DIGIADPCM" },
                {WaveFormatEncoding.ControlResCr10, "CONTYROL RES CR10" },
        };

        public List<float> mLeftRecord = new List<float>(); //  左部分抽出データ(リアルタイム表示用)
        public List<float> mRightRecord = new List<float>();//  右部分抽出データ(リアルタイム表示用)
        public float[] mSampleL;                            //  左FLOATデータ(ストリームデータから変換)
        public float[] mSampleR;                            //  右FLOATデータ(ストリームデータから変換)

        public string mPlayFile { get; protected set; }    //  演奏中のファイル
        public long mDataLength { get; protected set; }     //  データサイズ
        public TimeSpan mTotalTime { get; protected set; }  //  演奏時間
        public WaveFormat mWaveFormat { get; protected set; }//  Wave Format
        public enum PLAYSTAT { NON, PLAY, PAUSE, STOP }
        private PLAYSTAT mPlayStat;

        enum DATATYPE { NON, WAVE, MP3, FLAC, WMA }
        private DATATYPE mDataType = DATATYPE.NON;  //  演奏データの種類
        enum PLAYERTYPE { MEDIAPLAYER, NAUDIO }
        private PLAYERTYPE mPlayerType = PLAYERTYPE.NAUDIO;
        private MediaPlayer mMediaPlayer;               //  MediaPlayer
        private FlacReader mFlacReader;                 //  Flacデータの入力ストリーム
        private WMAFileReader mWMAFileReader;           //  WMAデータの入力ストリーム
        private AudioFileReader mNAudioReader;          //  NAudio の入力ストリーム
        private int mOutDeviceNo = 0;
        private WaveOut mWaveOut;                       //  NAudio の出力

        private WasapiLoopbackCapture mLoopBackWaveIn;  //  LoopbackCaptureクラス
        private MMDevice mDevice;                       //  出力デバイスデータ

        private YLib ylib = new YLib();


        public AudioLib()
        {
            mPlayStat = PLAYSTAT.NON;
        }

        public void dispose()
        {
            Stop();
            if (mWaveOut != null) {
                mWaveOut.Stop();
                mWaveOut.Dispose();
                mWaveOut = null;
            }
            if (mMediaPlayer != null)
                mMediaPlayer = null;
            if (mFlacReader != null)
                mFlacReader = null;
            if (mWMAFileReader != null)
                mWMAFileReader = null;
            if (mNAudioReader != null)
                mNAudioReader = null;
        }

        /// <summary>
        /// 再生デバイスの取得
        /// </summary>
        /// <returns></returns>
        public List<string> GetDevices()
        {
            List<string> deviceList = new List<string>();
            for (int i = 0; i < WaveOut.DeviceCount; i++) {
                var capabilities = WaveOut.GetCapabilities(i);
                deviceList.Add(capabilities.ProductName);
            }
            return deviceList;
        }

        /// <summary>
        /// 再生デバイスの設定
        /// </summary>
        /// <param name="deviceIndex">デバイスNo</param>
        public void setOutDevice(int deviceIndex)
        {
            mOutDeviceNo = deviceIndex;
            if (mWaveOut != null)
                mWaveOut.DeviceNumber = mOutDeviceNo;
        }

        /// <summary>
        /// 音声出力デバイスの取得
        /// https://taktak.jp/2017/03/07/1800
        /// </summary>
        /// <returns></returns>
        public List<string> getLoopBackDevice()
        {
            var collection = new MMDeviceEnumerator().EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
            List<string> deviceList = new List<string>();
            for (int i = 0; i < collection.Count; i++) {
                deviceList.Add(string.Format("{0} Channels {1}" + "\nSampleRate {2:#,0} BitsPerSample {3:#,0} BlockAlign {4}",
                    collection[i].FriendlyName, collection[i].AudioClient.MixFormat.Channels,
                    collection[i].AudioClient.MixFormat.SampleRate,
                    collection[i].AudioClient.MixFormat.BitsPerSample,
                    collection[i].AudioClient.MixFormat.BlockAlign));
            }
            return deviceList;
        }


        /// <summary>
        /// MediaPlayerとNAudioとの切り替え
        /// MediaPlayerを使用すると音声データの解析はできないが
        /// 音切れが提言する
        /// </summary>
        /// <param name="use"></param>
        public void setUseMediaPlayer(bool use)
        {
            mPlayerType = use ? PLAYERTYPE.MEDIAPLAYER : PLAYERTYPE.NAUDIO;
        }

        /// <summary>
        /// MediaPlayerの使用の有無
        /// </summary>
        /// <returns></returns>
        public bool getUseMediaPlayer()
        {
            return mPlayerType == PLAYERTYPE.MEDIAPLAYER ? true : false;
        }

        /// <summary>
        /// 音楽ファイルを開く
        /// </summary>
        /// <param name="fileName">ファイル名</param>
        /// <returns></returns>
        public bool Open(string fileName)
        {
            if (fileName.Length == 0 || !File.Exists(fileName))
                return false;
            if (!File.Exists(fileName))
                return false;
            mPlayFile = fileName;
            string ext = Path.GetExtension(fileName).ToLower();
            //  音楽ファイルの種類を設定
            if (ext.CompareTo(".flac") == 0) {
                mDataType = DATATYPE.FLAC;
            } else if (ext.CompareTo(".mp3") == 0) {
                mDataType = DATATYPE.MP3;
            } else if (ext.CompareTo(".wav") == 0) {
                mDataType = DATATYPE.WAVE;
            } else if (ext.CompareTo(".wma") == 0) {
                mDataType = DATATYPE.WMA;
            } else {
                return false;
            }
            Stop();
            if (mPlayerType == PLAYERTYPE.MEDIAPLAYER) {
                return MediaPlayerOpen(fileName);
            } else if (mPlayerType == PLAYERTYPE.NAUDIO) {
                return NAudioOpen(fileName);
            } else {
                return false;
            }
        }

        /// <summary>
        /// 演奏開始
        /// 開始位置が0以下の時前回の続き
        /// </summary>
        /// <param name="position">演奏開始位置</param>
        /// <returns></returns>
        public bool Play(long position)
        {
            if (mPlayerType == PLAYERTYPE.MEDIAPLAYER) {
                if (mMediaPlayer != null) {
                    if (0 <= position)
                        setCurPosition(position);
                    mPlayStat = PLAYSTAT.PLAY;
                    mMediaPlayer.Play();
                    return true;
                }
            } else if (mPlayerType == PLAYERTYPE.NAUDIO) {
                if (mWaveOut != null) {
                    if (0 <= position)
                        setCurPosition(position);
                    mPlayStat = PLAYSTAT.PLAY;
                    mWaveOut.Play();                    //  演奏開始
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 演奏停止
        /// </summary>
        /// <returns></returns>
        public bool Stop()
        {
            if (mPlayerType == PLAYERTYPE.MEDIAPLAYER) {
                if (mMediaPlayer != null) {
                    mMediaPlayer.Stop();
                    mPlayStat = PLAYSTAT.STOP;
                    setCurPosition(0);
                    return true;
                }
            } else if (mPlayerType == PLAYERTYPE.NAUDIO) {
                if (mWaveOut != null) {
                    mWaveOut.Stop();
                    mPlayStat = PLAYSTAT.STOP;
                    setCurPosition(0);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 演奏の一時停止
        /// </summary>
        /// <returns></returns>
        public long Pause()
        {
            if (mPlayerType == PLAYERTYPE.MEDIAPLAYER) {
                if (mMediaPlayer != null) {
                    mMediaPlayer.Pause();
                    mPlayStat = PLAYSTAT.PAUSE;
                    return getCurPosition();
                }
            } else if (mPlayerType == PLAYERTYPE.NAUDIO) {
                if (mWaveOut != null) {
                    mWaveOut.Pause();
                    mPlayStat = PLAYSTAT.PAUSE;
                    return getCurPosition();
                }
            }
            return 0;
        }

        /// <summary>
        /// MediaPlayerでファイルを開く
        /// </summary>
        /// <param name="fileName">ファイル名</param>
        /// <returns></returns>
        private bool MediaPlayerOpen(string fileName)
        {
            if (mMediaPlayer == null)
                mMediaPlayer = new MediaPlayer();
            mMediaPlayer.Open(new Uri(fileName));
            //  演奏時間の取得が有効になるまで待つ
            int count = 0;
            while (!mMediaPlayer.NaturalDuration.HasTimeSpan) {
                Thread.Sleep(100);
                if (100 < count++) {
                    mMediaPlayer.Stop();
                    return false;
                }
            }
            mDataLength = (long)mMediaPlayer.NaturalDuration.TimeSpan.TotalMilliseconds;
            mTotalTime = mMediaPlayer.NaturalDuration.TimeSpan;
            mMediaPlayer.Position = TimeSpan.Zero;
            mWaveFormat = getWaveFormat(fileName);
            return true;
        }

        /// <summary>
        /// NAudioでファイルを開く
        /// </summary>
        /// <param name="fileName">ファイル名</param>
        /// <returns></returns>
        private bool NAudioOpen(string fileName)
        {
            try {
                mWaveOut = new WaveOut();                           //  NAudioの出力
                if (mDataType == DATATYPE.FLAC) {
                    //  FLACデータの取り込み
                    mFlacReader = new FlacReader(fileName);
                    mWaveOut.DeviceNumber = mOutDeviceNo;
                    mWaveOut.Init(mFlacReader);                     //  出力の設定
                    mDataLength = mFlacReader.Length;
                    mTotalTime = mFlacReader.TotalTime;
                    mFlacReader.Position = 0;
                    mWaveFormat = mFlacReader.WaveFormat;
                } else if (mDataType == DATATYPE.WMA) {
                    //  WMAデータの取り込み
                    mWMAFileReader = new WMAFileReader(fileName);   //  音楽ファイルの読み込み
                    mWaveOut.DeviceNumber = mOutDeviceNo;
                    mWaveOut.Init(mWMAFileReader);                  //  出力の設定
                    mDataLength = mWMAFileReader.Length;
                    mTotalTime = mWMAFileReader.TotalTime;
                    mWMAFileReader.Position = 0;
                    mWaveFormat = mWMAFileReader.WaveFormat;
                } else if (mDataType == DATATYPE.MP3 || mDataType == DATATYPE.WAVE) {
                    //  MP3,WAVデータの取り込み
                    mNAudioReader = new AudioFileReader(fileName);  //  音楽ファイルの読み込み
                    mWaveOut.DeviceNumber = mOutDeviceNo;
                    mWaveOut.Init(mNAudioReader);                   //  出力の設定
                    mDataLength = mNAudioReader.Length;
                    mTotalTime = mNAudioReader.TotalTime;
                    mNAudioReader.Position = 0;
                    mWaveFormat = mNAudioReader.WaveFormat;
                }
            } catch (Exception e) {
                MessageBox.Show(fileName + "\n" + e.Message);
                return false;
            }
            if (mNAudioReader == null && mFlacReader == null && mWMAFileReader == null)
                return false;
            return true;
        }

        /// <summary>
        /// 演奏状態が停止状態か確認
        /// </summary>
        /// <returns></returns>
        public bool IsStopped()
        {
            return getPlayerStat() == PLAYSTAT.STOP;
        }

        /// <summary>
        /// 演奏状態が一時停止かを確認
        /// </summary>
        /// <returns></returns>
        public bool IsPause()
        {
            return getPlayerStat() == PLAYSTAT.PAUSE;
        }

        /// <summary>
        /// 演奏状態かを確認
        /// </summary>
        /// <returns></returns>
        public bool IsPlaying()
        {
            return getPlayerStat() == PLAYSTAT.PLAY;
        }


        /// <summary>
        /// Playerの状態を取得
        /// </summary>
        /// <returns></returns>
        public PLAYSTAT getPlayerStat()
        {
            if (mPlayerType == PLAYERTYPE.NAUDIO) {
                if (mWaveOut != null) {
                    if (mWaveOut.PlaybackState == PlaybackState.Paused) {
                        mPlayStat = PLAYSTAT.PAUSE;
                    } else if (mWaveOut.PlaybackState == PlaybackState.Stopped) {
                        mPlayStat = PLAYSTAT.STOP;
                    } else if (mWaveOut.PlaybackState == PlaybackState.Playing) {
                        mPlayStat = PLAYSTAT.PLAY;
                    } else {
                        mPlayStat = PLAYSTAT.NON;
                    }
                } else {
                    mPlayStat = PLAYSTAT.NON;
                }
            } else if (mPlayerType == PLAYERTYPE.MEDIAPLAYER) {
                if (mMediaPlayer!= null) {
                    if (mMediaPlayer.Position == mMediaPlayer.NaturalDuration) {
                        mPlayStat = PLAYSTAT.STOP;
                    }
                } else {
                    mPlayStat = PLAYSTAT.NON;
                }
            }
            return mPlayStat;
        }

        /// <summary>
        /// 演奏開始位置の設定
        /// NAudioはデータのアクセス位置/MediaPlayerは演奏位置時間(msec)
        /// </summary>
        /// <param name="position"></param>
        public void setCurPosition(long position)
        {
            if (mPlayerType == PLAYERTYPE.NAUDIO) {
                if (mDataType == DATATYPE.FLAC && mFlacReader != null) {
                    mFlacReader.Position = position;
                } else if (mDataType == DATATYPE.WMA && mWMAFileReader != null) {
                    mWMAFileReader.Position = position;
                } else if ((mDataType == DATATYPE.MP3 || mDataType == DATATYPE.WAVE) && mNAudioReader != null) {
                    mNAudioReader.Position = position;
                }
            } else if (mPlayerType == PLAYERTYPE.MEDIAPLAYER) {
                mMediaPlayer.Position = TimeSpan.FromMilliseconds((double)position);
            }
        }

        /// <summary>
        /// 演奏位置の取得
        /// </summary>
        /// <returns></returns>
        public long getCurPosition()
        {
            if (mPlayerType == PLAYERTYPE.NAUDIO) {
                if (mDataType == DATATYPE.FLAC) {
                    return mFlacReader.Position;
                } else if (mDataType == DATATYPE.WMA) {
                    return mWMAFileReader.Position;
                } else if (mDataType == DATATYPE.MP3 || mDataType == DATATYPE.WAVE) {
                    return mNAudioReader.Position;
                }
            } else if (mPlayerType == PLAYERTYPE.MEDIAPLAYER) {
                return (long)mMediaPlayer.Position.TotalMilliseconds;
            }
            return 0;
        }

        /// <summary>
        /// 演奏の位置を時間で設定
        /// </summary>
        /// <param name="position"></param>
        public void setCurPositionSecond(double position)
        {
            if (mPlayerType == PLAYERTYPE.MEDIAPLAYER) {
                mMediaPlayer.Position = TimeSpan.FromSeconds(position);
            } else if (mPlayerType == PLAYERTYPE.NAUDIO) {
                if (mDataType == DATATYPE.FLAC) {
                    mFlacReader.Position = (long)(position / mFlacReader.TotalTime.TotalSeconds *  mFlacReader.Length);
                } else if (mDataType == DATATYPE.WMA ) {
                    mWMAFileReader.Position = (long)(position / mWMAFileReader.TotalTime.TotalSeconds * mWMAFileReader.Length);
                } else if (mDataType == DATATYPE.MP3 || mDataType == DATATYPE.WAVE) {
                   mNAudioReader.Position = (long)(position / mNAudioReader.TotalTime.TotalSeconds * mNAudioReader.Length);
                }
            }
        }

        /// <summary>
        /// 演奏の位置を時間で取得(秒)
        /// </summary>
        /// <returns></returns>
        public double getCurPositionSecond()
        {
            if (mPlayerType == PLAYERTYPE.MEDIAPLAYER && mMediaPlayer != null) {
                return mMediaPlayer.Position.TotalSeconds;
            } else if (mPlayerType == PLAYERTYPE.NAUDIO) {
                if (mDataType == DATATYPE.FLAC && mFlacReader != null) {
                    return mFlacReader.TotalTime.TotalSeconds / mFlacReader.Length * mFlacReader.Position;
                } else if (mDataType == DATATYPE.WMA && mWMAFileReader != null) {
                    return mWMAFileReader.TotalTime.TotalSeconds / mWMAFileReader.Length * mWMAFileReader.Position;
                } else if (mDataType == DATATYPE.MP3 || mDataType == DATATYPE.WAVE && mNAudioReader!= null) {
                    return mNAudioReader.TotalTime.TotalSeconds / mNAudioReader.Length * mNAudioReader.Position;
                }
            }
            return 0;
        }

        /// <summary>
        /// ストリームの位置をFLOATデータ位置に変換する
        /// </summary>
        /// <param name="streamPosition">ストリームの位置</param>
        /// <returns></returns>
        public int streamPos2FloatDataPos(long streamPosition)
        {
            return (int)(streamPosition * mSampleL.Length / mDataLength);
        }

        /// <summary>
        /// FLOATデータの位置をストリームの位置に変換
        /// </summary>
        /// <param name="pos">FLOATデータ位置</param>
        /// <returns></returns>
        public long floatDataPos2StreamPos(int pos)
        {
            return pos * mDataLength / mSampleL.Length;
        }

        /// <summary>
        /// floatデータの位置から演奏位置時間(秒)を求める
        /// </summary>
        /// <param name="floatDataPos"></param>
        /// <returns></returns>
        public double floatDataPos2Second(int floatDataPos)
        {
            return mTotalTime.TotalSeconds * floatDataPos / mSampleL.Length;
        }


        /// <summary>
        /// 現在の音量(max 1.0f)
        /// </summary>
        /// <returns></returns>
        public float getVolume()
        {
            if (mPlayerType == PLAYERTYPE.MEDIAPLAYER && mMediaPlayer != null) {
                return (float)mMediaPlayer.Volume;
            } else if (mPlayerType == PLAYERTYPE.NAUDIO) {
                return mWaveOut.Volume;
            }
            return 0;
        }

        /// <summary>
        /// 音量の設定(0～1)
        /// </summary>
        /// <param name="vol"></param>
        public void setVolume(float vol)
        {
            if (mPlayerType == PLAYERTYPE.MEDIAPLAYER && mMediaPlayer != null) {
                mMediaPlayer.Volume = vol;
            } else if (mPlayerType == PLAYERTYPE.NAUDIO) {
                mWaveOut.Volume = vol;
            }
        }

        /// <summary>
        /// 現在のバランス位置
        /// </summary>
        /// <returns></returns>
        public float getBalance()
        {
            if (mPlayerType == PLAYERTYPE.MEDIAPLAYER && mMediaPlayer != null) {
                return (float)mMediaPlayer.Balance;
            }
            return 0;
        }

        /// <summary>
        /// バランスの設定(-1～１)
        /// </summary>
        /// <param name="balance"></param>
        public void setBalance(float balance)
        {
            if (mPlayerType == PLAYERTYPE.MEDIAPLAYER && mMediaPlayer != null) {
                mMediaPlayer.Balance = balance;     //  -1 ～ 1の範囲
            }
        }

        /// <summary>
        /// WaveFormatの取得
        /// 内容
        /// AverageBytesPerSecond : 1秒あたりの平均バイト数 (SampleRate * BlockAlignに等しい)
        /// BitsPerSample : サンプルあたりのビット数 量子化ビット数 (たいてい16か32、ときに24か8)
        /// BlockAlign : ブロック アラインメント (データの最小単位)
        /// Channels : チャンネル数 (1=モノラル、2=ステレオなど)
        /// Encoding : エンコードの種類(おもにPcm(WAVE_FORMAT_PCM)/IeeeFloat(WAVE_FORMAT_IEEE_FLOAT))
        /// ExtraSize : 波形が使用する余分なバイト数(WAVEFORMATEX ヘッダの後に余分なデータを保存する圧縮フォーマットを除いて多くの場合は 0 )
        /// SampleRate : サンプリング周波数 (サンプリングレート)  (1秒あたりのサンプル数)
        /// </summary>
        /// <param name="fileName">ファイル名</param>
        /// <returns></returns>
        public WaveFormat getWaveFormat(string fileName)
        {
            try {
                if (mDataType == DATATYPE.FLAC) {
                    //  FLACデータの取り込み
                    FlacReader flacReader = new FlacReader(fileName);
                    mDataLength = flacReader.Length;
                    return flacReader.WaveFormat;
                } else if (mDataType == DATATYPE.WMA) {
                    WMAFileReader wMAFileReader = new WMAFileReader(fileName);
                    mDataLength = wMAFileReader.Length;
                    return wMAFileReader.WaveFormat;
                } else if (mDataType == DATATYPE.MP3 || mDataType == DATATYPE.WAVE) {
                    //  MP3,WAVデータの取り込み
                    AudioFileReader nAudioReader = new AudioFileReader(fileName);
                    mDataLength = nAudioReader.Length;
                    return nAudioReader.WaveFormat;
                }
            } catch (Exception e) {
                MessageBox.Show(fileName + "\n" + e.Message);
                return null;
            }
            return null;
        }

        /// <summary>
        ///  1秒あたりの平均バイト数 (SampleRate * BlockAlignに等しい)
        /// </summary>
        /// <returns></returns>
        public int getAverageBytesPerSecond()
        {
            return mWaveFormat==null ? 0 : mWaveFormat.AverageBytesPerSecond;
        }

        /// <summary>
        /// サンプルあたりのビット数 量子化ビット数 (たいてい16か32、ときに24か8)
        /// </summary>
        /// <returns></returns>
        public int getBitsPerSample()
        {
            return mWaveFormat == null ? 0 : mWaveFormat.BitsPerSample;
        }

        /// <summary>
        /// ブロック アラインメント (データの最小単位)
        /// </summary>
        /// <returns></returns>
        public int getBlockAlign()
        {
            return mWaveFormat == null ? 0 : mWaveFormat.BlockAlign;
        }

        /// <summary>
        /// チャンネル数 (1=モノラル、2=ステレオなど)
        /// </summary>
        /// <returns></returns>
        public int getChannels()
        {
            return mWaveFormat == null ? 0 : mWaveFormat.Channels;
        }

        /// <summary>
        /// エンコードの種類(おもにPcm(WAVE_FORMAT_PCM)/IeeeFloat(WAVE_FORMAT_IEEE_FLOAT))
        /// </summary>
        /// <returns></returns>
        public string getEncording()
        {
            if (mWaveFormat == null)
                return "";
            else
                return mEncording.ContainsKey(mWaveFormat.Encoding) ?
                    mEncording[mWaveFormat.Encoding] : mEncording[WaveFormatEncoding.Unknown];
        }

        /// <summary>
        /// 波形が使用する余分なバイト数
        /// (WAVEFORMATEX ヘッダの後に余分なデータを保存する圧縮フォーマットを除いて多くの場合は 0 )
        /// </summary>
        /// <returns></returns>
        public int getExtraSize()
        {
            return mWaveFormat == null ? 0 : mWaveFormat.ExtraSize;
        }

        /// <summary>
        /// サンプリング周波数 (サンプリングレート)  (1秒あたりのサンプル数)
        /// </summary>
        /// <returns></returns>
        public int getSampleRate()
        {
            return mWaveFormat == null ? 0 : mWaveFormat.SampleRate;
        }

        /// <summary>
        /// Readerデータをbyteから2Channelsのfloatデータに変換する
        /// </summary>
        public void byte2floatReaderData()
        {
            if (mPlayerType == PLAYERTYPE.NAUDIO) {
                if (mDataType == DATATYPE.FLAC) {
                    flacByte2FloatData(mFlacReader);
                    mFlacReader.Position = 0;
                } else if (mDataType == DATATYPE.WMA) {
                    wmaByte2FloatData(mWMAFileReader);
                    mWMAFileReader.Position = 0;
                } else if (mDataType == DATATYPE.MP3 || mDataType == DATATYPE.WAVE) {
                    mp3Byte2FloatData(mNAudioReader);
                    mNAudioReader.Position = 0;
                }
            }
        }

        /// <summary>
        /// FLACの波形データの取得
        /// </summary>
        /// <param name="reader">ストリームデータ</param>
        private void flacByte2FloatData(FlacReader reader)
        {
            byte[] buffer = new byte[reader.Length];
            int bytesRead = reader.Read(buffer, 0, buffer.Length);
            byte2FloatData(buffer, bytesRead, reader.BlockAlign, reader.WaveFormat.BitsPerSample);
        }

        /// <summary>
        /// WMAの波形データ取得
        /// </summary>
        /// <param name="reader"></param>
        private void wmaByte2FloatData(WMAFileReader reader)
        {
            byte[] buffer = new byte[reader.Length];
            int bytesRead = reader.Read(buffer, 0, buffer.Length);
            byte2FloatData(buffer, bytesRead, reader.BlockAlign, reader.WaveFormat.BitsPerSample);
        }

        /// <summary>
        /// MP3,WAVE波形データの取得
        /// </summary>
        /// <param name="reader">ストリームデータ</param>
        private void mp3Byte2FloatData(AudioFileReader reader)
        {
            byte[] buffer = new byte[reader.Length];
            int bytesRead = reader.Read(buffer, 0, buffer.Length);
            byte2FloatData(buffer, bytesRead, reader.BlockAlign, reader.WaveFormat.BitsPerSample);
        }

        /// <summary>
        /// ストリームデータをfloat配列に変換
        /// </summary>
        /// <param name="buffer">ストリームデータ</param>
        /// <param name="bytesRead">ストリームデータサイズ(byte)</param>
        /// <param name="blockAlign">ブロックアライメント</param>
        /// <param name="bitsPerSample">ビットサイズ</param>
        private void byte2FloatData(byte[] buffer, int bytesRead, int blockAlign, int bitsPerSample)
        {
            mSampleL = new float[bytesRead / blockAlign];
            mSampleR = new float[bytesRead / blockAlign];

            switch (bitsPerSample) {
                case 8:         //  BlockAlign = 2, PCM
                    for (int i = 0; i < mSampleL.Length; i++) {
                        mSampleL[i] = (buffer[i * blockAlign] - 128) / 128f;
                        mSampleR[i] = (buffer[i * blockAlign + blockAlign / 2] - 128) / 128f;
                    }
                    break;
                case 16:        //  BlockAlign = 4, PCM
                    for (int i = 0; i < mSampleL.Length; i++) {
                        mSampleL[i] = BitConverter.ToInt16(buffer, i * blockAlign) / 32768f;
                        mSampleR[i] = BitConverter.ToInt16(buffer, i * blockAlign + blockAlign / 2) / 32768f;
                    }
                    break;
                case 24:        //  BlockAlign = 6, PCM
                    byte[] bufL = new byte[4];
                    byte[] bufR = new byte[4];
                    for (int i = 0; i < mSampleL.Length; i++) {
                        Array.Copy(buffer, i * blockAlign, bufL, 1, 3);
                        Array.Copy(buffer, i * blockAlign + blockAlign / 2, bufR, 1, 3);
                        mSampleL[i] = BitConverter.ToInt32(bufL, 0) / 256f / 8388608f;
                        mSampleR[i] = BitConverter.ToInt32(bufR, 0) / 256f / 8388608f;
                    }
                    break;
                case 32:        //  BlockAlign = 8, IEEE_FLOAT
                    for (int i = 0; i < mSampleL.Length; i++) {
                        mSampleL[i] = BitConverter.ToSingle(buffer, i * blockAlign);
                        mSampleR[i] = BitConverter.ToSingle(buffer, i * blockAlign + blockAlign / 2);
                    }
                    break;
            }
        }

        /// <summary>
        /// 生のfloatデータから表示用のデータを取り出しmLeftRecordとmRightRecordに格納する
        /// 表示用のサンプル周波数でデータを間引きと1フレーム分のデータ数を取る
        /// </summary>
        /// <param name="dispBitsRate">表示用のサンプル周波数</param>
        /// <param name="xRange"> 1フレーム分のデータ数</param>
        public void setSampleData(int dispBitsRate, double xRange)
        {
            mLeftRecord.Clear();
            mRightRecord.Clear();
            if (mSampleL == null || mSampleL.Length == 0)
                return;
            //  byte位置をfloat配列の位置に変換
            int mBitsRateInterval = mWaveFormat.SampleRate / dispBitsRate;  //  表示用にデータをまびきする間隔
            long start = (long)((double)mSampleL.Length * (double)getCurPosition() / (double)mDataLength);
            start = start < 0 ? 0 : start;
            long end = start + (long)xRange * mBitsRateInterval;
            for (long i = start; i < end && i < mSampleL.Length; i += mBitsRateInterval) {
                mLeftRecord.Add(mSampleL[i]);
                mRightRecord.Add(mSampleR[i]);
            }
        }

        /// <summary>
        /// 高速フーリエ変換(NAudio.Dspを使用)
        /// </summary>
        /// <param name="sampleRecord">音データ</param>
        /// <param name="dispBitsRate">サンプリング周波数</param>
        /// <returns></returns>
        public List<System.Numerics.Complex> DoFourier(List<float> sampleRecord, int dispBitsRate)
        {
            var fftsample = new NAudio.Dsp.Complex[sampleRecord.Count];

            //ハミング窓をかける
            for (int i = 0; i < sampleRecord.Count; i++) {
                fftsample[i].X = (float)(sampleRecord[i] * FastFourierTransform.HammingWindow(i, sampleRecord.Count));
                fftsample[i].Y = 0.0f;
            }

            //サンプル数のlogを取る
            var m = (int)Math.Log(fftsample.Length, 2);
            //FFT
            FastFourierTransform.FFT(true, m, fftsample);

            //結果を出力
            //FFTSamplenum / 2なのは標本化定理から半分は冗長だから
            List<System.Numerics.Complex> dataPoints = new List<System.Numerics.Complex>();
            var s = sampleRecord.Count * (1.0 / dispBitsRate);
            for (int k = 0; k < fftsample.Length / 2; k++) {
                //複素数の大きさを計算
                double diagonal = Math.Sqrt(fftsample[k].X * fftsample[k].X + fftsample[k].Y * fftsample[k].Y);
                dataPoints.Add(new System.Numerics.Complex((double)k / s, diagonal * 500.0)); //  FftTrandForms()とレベルを合わせるために500倍している
            }

            return dataPoints;
        }

        /// <summary>
        /// ローパスフィルタ(双二次フィルタ NAudio.Dsp)
        /// </summary>
        /// <param name="sampleRecord">サンプリングデータ</param>
        /// <param name="dispBitsRate">サンプリング周波数</param>
        /// <param name="cutFreq">カット周波数</param>
        public void LowPassfilter(List<float> sampleRecord, int dispBitsRate, float cutFreq)
        {
            //  BiQuadFilter LowPassFilter(float sampleRate, float cutoffFrequency, float q);
            BiQuadFilter filter = BiQuadFilter.LowPassFilter(dispBitsRate, cutFreq, 1);
            for (int i = 0; i < sampleRecord.Count; i++)
                sampleRecord[i] = filter.Transform(sampleRecord[i]);
        }

    }
}
