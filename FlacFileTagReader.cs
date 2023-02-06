using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfLib;

namespace AudioLib
{
    class FlacFileTagReader
    {
        //  画像データの種別
        private readonly Dictionary<int, string> mPictureType = new Dictionary<int, string>() {
            {0x00, "Other"},
            {0x01, "32x32 pixels 'file icon' (PNG only)"},
            {0x02, "Other file icon"},
            {0x03, "Cover (front)"},
            {0x04, "Cover (back)"},
            {0x05, "Leaflet page"},
            {0x06, "Media (e.g. lable side of CD)"},
            {0x07, "Lead artist/lead performer/soloist"},
            {0x08, "Artist/performer"},
            {0x09, "Conductor"},
            {0x0A, "Band/Orchestra"},
            {0x0B, "Composer"},
            {0x0C, "Lyricist/text writer"},
            {0x0D, "Recording Location"},
            {0x0E, "During recording"},
            {0x0F, "During performance"},
            {0x10, "Movie/video screen capture"},
            {0x11, "A bright coloured fish"},
            {0x12, "Illustration"},
            {0x13, "Band/artist logotype"},
            {0x14, "Publisher/Studio logotype"},
        };

        private byte[] mFlacTagHeader = new byte[4];

        public string mEncording = "";  //   エンコードの種類(おもにPcm(WAVE_FORMAT_PCM)/IeeeFloat(WAVE_FORMAT_IEEE_FLOAT))
        public int mChanneles = 0;      //  チャンネル数 (1=モノラル、2=ステレオなど)
        public int mBitPerSample = 0;   //  量子化ビット数 (たいてい16か32、ときに24か8)
        public int mBlockAlign = 0;     //  ブロック アラインメント (データの最小単位)
        public int mSampleRate = 0;     //  サンプリング周波数 (サンプリングレート)  (1秒あたりのサンプル数)
        public int mSampleBitRate = 0;  //  サンプルビットレート(16-320kbps)（1秒あたりのデータ量）
        public long mDataLength = 0;    //  データサイズ(ファイルサイズ)
        public long mPlayLength;        //  再生時間(msec)

        public long mTagSize = 0;
        public List<string> mFlacTagList = new List<string>();
        public Dictionary<string, string> mFlacTags = new Dictionary<string, string>();
        public List<ImageData> mImageData = new List<ImageData>();

        private FileStream mFileStream;
        private YLib ylib = new YLib();

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public FlacFileTagReader()
        {

        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="path">ファイルパス</param>
        public FlacFileTagReader(string path)
        {
            FileTagReader(path);
        }

        /// <summary>
        /// ファイルデータの取得
        /// </summary>
        /// <param name="path">ファイルパス</param>
        /// <returns>データ取得有無</returns>
        public bool FileTagReader(string path)
        {
            if (!File.Exists(path))
                return false;

            if (Path.GetExtension(path).ToLower().CompareTo(".flac") == 0) {
                //  FLACデータファイルのタグ情報取得
                using (mFileStream = new FileStream(path, FileMode.Open, FileAccess.Read)) {
                    int readBytes = mFileStream.Read(mFlacTagHeader, 0, mFlacTagHeader.Length);
                    mTagSize += readBytes;
                    if (readBytes == mFlacTagHeader.Length) {
                        string tagHeadText = Encoding.ASCII.GetString(mFlacTagHeader);
                        if (tagHeadText.CompareTo("fLaC") == 0) {
                            getFlacTag();      //  MetaDataBlock
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// FLACファイルのタグ情報取得
        /// FLACフォーマット https://xiph.org/flac/format.html
        /// FLAC  Vorbis Commentのフィールド名 https://www.xiph.org/vorbis/doc/v-comment.html
        /// </summary>
        private void getFlacTag()
        {
            bool lastData = false;
            byte[] metaDataBlockHeader = new byte[4];
            mFlacTagList.Clear();
            while (!lastData) {
                //  Metadata Block Headerの読み込み(4byte)
                int readBytes = mFileStream.Read(metaDataBlockHeader, 0, metaDataBlockHeader.Length);
                mTagSize += readBytes;
                if (readBytes != 4) {
                    //  読込終了
                    lastData = true;
                } else {
                    //ylib.binaryDump(metaDataBlockHeader, 0, readBytes, "MetaDataBlockHeader");
                    byte lf = (byte)(metaDataBlockHeader[0] & 0x80);
                    lastData = lf == 0 ? false : true;
                    int blockType = metaDataBlockHeader[0] & 0x7f;
                    int blockSize = (int)ylib.bitReverseConvertLong(metaDataBlockHeader, 1, 3);
                    //System.Diagnostics.Debug.WriteLine("{0} {1} {2} {3}", lf, lastData, blockType, blockSize);
                    byte[] buffer = new byte[blockSize];
                    readBytes = mFileStream.Read(buffer, 0, blockSize);
                    mTagSize += readBytes;
                    if (readBytes == blockSize) {
                        switch (blockType) {
                            case 0:
                                getFlacStreamInfo(buffer);  //  ストリーム情報
                                break;
                            case 1:
                                getFlacPadding(buffer);     //  パディング(予約領域:ブロック内容に意味なし)
                                break;
                            case 2:
                                getFlacAppiication(buffer); //  アプリケーション領域(サードパーティーが使用)
                                break;
                            case 3:
                                getFlacSeektable(buffer);   //  シークポイントテーブル
                                break;
                            case 4:
                                getFlacVorbisComment(buffer);   //  テキストコメント
                                break;
                            case 5:
                                getFlacCuesheet(buffer);    //  
                                break;
                            case 6:
                                getFlacPicture(buffer);     //  画像データ
                                break;
                            default:
                                lastData = true;
                                break;
                        }
                    } else {
                        lastData = true;
                    }
                }
            }
        }

        /// <summary>
        /// FLAC ストリームデータ情報
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        private bool getFlacStreamInfo(byte[] buffer)
        {
            //ylib.binaryDump(buffer, 0, buffer.Length, "getFlacStreamInfo");
            //  最小ブロックサイズ
            int index = 0;
            int blockSizeMin = (int)ylib.bitReverseConvertLong(buffer, index, 2);
            //  最大ブロックサイズ
            index += 2;
            int blockSizeMax = (int)ylib.bitReverseConvertLong(buffer, index, 2);
            //  フレーム最小サイズ
            index += 2;
            int frameSizeMin = (int)ylib.bitReverseConvertLong(buffer, index, 3);
            //  フレーム最大サイズ
            index += 3;
            int frameSizeMax = (int)ylib.bitReverseConvertLong(buffer, index, 3);
            //  サンプル周波数
            index += 3;
            int sampleRat = (int)ylib.bitConvertBit(buffer, index * 8, 20);
            //  チャンネル数(channels - 1)
            int channels = (int)ylib.bitConvertBit(buffer, index * 8 + 20, 3) + 1;
            //  量子化ビット(4～32bit)(bit/sample - 1)
            int bitPerSample = (int)ylib.bitConvertBit(buffer, index * 8 + 20 + 3, 5) + 1;
            //  サンプリングのデータ数
            int totalSamples = (int)ylib.bitConvertBit(buffer, index * 8 + 20 + 3 + 5, 36);
            //  音楽データのMD5値
            index += 8;
            byte[] md5Signature = new byte[16];
            Array.Copy(buffer, index, md5Signature, 0, 16);
            string md5 = ylib.binary2HexString(md5Signature, 0, 16);

            //System.Diagnostics.Debug.WriteLine("blockSize {0} {1} frameSize {2} {3} SampleRate {4:#,0} channels {5} bit/sample {6} totalSamples {7:#,0}",
            //    blockSizeMin, blockSizeMax, frameSizeMin, frameSizeMax, sampleRat, channels, bitPerSample, totalSamples);
            //ylib.binaryDump(md5Signature, 0, 16, "MD5");

            mFlacTagList.Add("[FLAC Stream Info]");
            mFlacTagList.Add("BlockSizeMin: " + blockSizeMin.ToString("#,0"));
            mFlacTagList.Add("BlockSizeMax: " + blockSizeMax.ToString("#,0"));
            mFlacTagList.Add("FrameSizeMin: " + frameSizeMin.ToString("#,0"));
            mFlacTagList.Add("FrameSizeMax: " + frameSizeMax.ToString("#,0"));
            mFlacTagList.Add("SampleRate  : " + sampleRat.ToString("#,0") + "Hz");
            mFlacTagList.Add("Channels    : " + channels.ToString());
            mFlacTagList.Add("BitPerSample: " + bitPerSample.ToString());
            mFlacTagList.Add("TotalSamples: " + totalSamples.ToString("#,0"));
            mFlacTagList.Add("MD5: " + md5);

            mChanneles = channels;
            mBitPerSample = bitPerSample;
            mSampleRate = sampleRat;

            return true;
        }

        /// <summary>
        /// パディングデータ
        /// 予約領域企(領域確保用)
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        private bool getFlacPadding(byte[] buffer)
        {
            mFlacTagList.Add("[Flac Padding]");
            //ylib.binaryDump(buffer, 0, Math.Min(buffer.Length, 32), "getFlacPadding");
            return false;
        }

        /// <summary>
        /// アプリケーションデータ
        /// カスタムアプリケーション用
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        private bool getFlacAppiication(byte[] buffer)
        {
            mFlacTagList.Add("[Flac Appiication]");
            //ylib.binaryDump(buffer, 0, Math.Min(buffer.Length, 32), "getFlacAppiication");
            return false;
        }

        /// <summary>
        /// シークテーブルデータ
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        private bool getFlacSeektable(byte[] buffer)
        {
            mFlacTagList.Add("[Flac Seektable]");
            //ylib.binaryDump(buffer, 0, Math.Min(buffer.Length, 32), "getFlacSeektable");
            return false;
        }

        /// <summary>
        /// FLAC Vorbis Comment データ
        /// UTF8のテキストコメント
        /// フィールドはタイトルと内容を"="で結んでいる
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        private bool getFlacVorbisComment(byte[] buffer)
        {
            //ylib.binaryDump(buffer, 0, Math.Min(buffer.Length, 32), "getFlacVorbisComment");
            mFlacTagList.Add("[Flac Vorbis Comment]");
            Encoding encoding = Encoding.UTF8;
            //  コメント
            int index = 0;
            int size = BitConverter.ToInt32(buffer, index);
            index += 4;
            string content = encoding.GetString(buffer, index, size);
            //System.Diagnostics.Debug.WriteLine("{0} {1}: {2}", index, size, content);
            mFlacTagList.Add(content);
            index += size + 4;
            while (index < buffer.Length) {
                //  タイトル名
                size = BitConverter.ToInt32(buffer, index);
                index += 4;
                content = encoding.GetString(buffer, index, size);
                mFlacTagList.Add(content);
                //System.Diagnostics.Debug.WriteLine("{0} {1}: {2}", index, size, content);
                index += size;
                int n = content.IndexOf("=");
                //  内容
                if (0 < n) {
                    string title = content.Substring(0, n).ToUpper();
                    string description = content.Substring(n + 1);
                    if (mFlacTags.ContainsKey(title)) {
                        mFlacTags[title] += "," + description;
                    } else {
                        mFlacTags.Add(title, description);
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// CueShett データ
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        private bool getFlacCuesheet(byte[] buffer)
        {
            mFlacTagList.Add("[Flac Cuesheet]");
            //ylib.binaryDump(buffer, 0, Math.Min(buffer.Length, 32), "getFlacCuesheet");
            return false;
        }

        private bool getFlacTrack(byte[] buffer)
        {
            mFlacTagList.Add("[Flac Track]");
            //ylib.binaryDump(buffer, 0, Math.Min(buffer.Length, 32), "getFlacTrack");
            return false;
        }

        private bool getFlacTrackIndex(byte[] buffer)
        {
            mFlacTagList.Add("[Flac TrackIndex]");
            //ylib.binaryDump(buffer, 0, Math.Min(buffer.Length, 32), "getFlacTrackIndex");
            return false;
        }

        /// <summary>
        /// FLAC 画像データ
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        private bool getFlacPicture(byte[] buffer)
        {
            //ylib.binaryDump(buffer, 0, Math.Min(buffer.Length, 64), "getFlacPicture");
            Encoding encoding = Encoding.ASCII;
            //  画像の種類
            int index = 0;
            int pictureType = (int)ylib.bitReverseConvertLong(buffer, index, 4);
            //  MIMEタイプ
            index += 4;
            int MIMEsize = (int)ylib.bitReverseConvertLong(buffer, index, 4);
            index += 4;
            string MIMEtypeString = encoding.GetString(buffer, index, MIMEsize);
            //  画像の説明
            index += MIMEsize;
            int discriptionSize = (int)ylib.bitReverseConvertLong(buffer, index, 4);
            index += 4;
            encoding = Encoding.UTF8;
            string discription = encoding.GetString(buffer, index, discriptionSize);
            //  画像の幅、高さ、色深度、色の数
            index += discriptionSize;
            int widthPixels = (int)ylib.bitReverseConvertLong(buffer, index, 4);
            index += 4;
            int heightPixels = (int)ylib.bitReverseConvertLong(buffer, index, 4);
            index += 4;
            int colorDepth = (int)ylib.bitReverseConvertLong(buffer, index, 4);
            index += 4;
            int colorUsedNumber = (int)ylib.bitReverseConvertLong(buffer, index, 4);
            //  画像データのサイズ
            index += 4;
            int pictureSize = (int)ylib.bitReverseConvertLong(buffer, index, 4);
            index += 4;

            mFlacTagList.Add("[Flac Picture]");
            mFlacTagList.Add("画像の種類: " + mPictureType[pictureType]);
            mFlacTagList.Add("MIMEタイプ: " + MIMEtypeString);
            mFlacTagList.Add("画像の説明: " + discription);
            mFlacTagList.Add("画像サイズ: " + widthPixels + " X " + heightPixels + " X " + colorDepth);

            //System.Diagnostics.Debug.WriteLine("{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10}",
            //    pictureType, mPictureType[pictureType],
            //    MIMEsize, MIMEtypeString, discriptionSize, discription,
            //    widthPixels, heightPixels, colorDepth, colorUsedNumber, pictureSize);

            //  画像データをImageDataクラスに入れる
            ImageData imageData = new ImageData();
            imageData.Description = discription;
            imageData.FileExt = "";
            imageData.ImageType = MIMEtypeString;
            imageData.PictureType = mPictureType[pictureType];
            imageData.PictureData = new byte[pictureSize];
            for (int j = 0; j < pictureSize; j++)
                imageData.PictureData[j] = buffer[index + j];
            //  画像リストに登録
            mImageData.Add(imageData);

            return true;
        }
    }
}
