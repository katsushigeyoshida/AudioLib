using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using WpfLib;

namespace AudioLib
{
    class NameGUID
    {
        public string Name { set; get; }
        public byte[] GUID { set; get; }

        public NameGUID(string name, byte[] guid)
        {
            Name = name;
            GUID = guid;
        }
    }

    class AsfFileTagReader
    {
        //  WMAファイルのタグの読み込み
        //  WMAファイルはASF(Advanced System Format)でタグが構成されている
        //  ASFの仕様  https://hwiegman.home.xs4all.nl/fileformats/asf/ASF_Specification.pdf
        //  解説 http://drang.s4.xrea.com/program/tips/id3tag/tag_wmp.html
        //       http://drang.s4.xrea.com/program/tips/id3tag/wmp/03_asf_top_level_header_object.html

        //  ASF Top level ASF object GUIDs
        private byte[] ASF_Header_Object =
            { 0x30, 0x26, 0xB2, 0x75, 0x8E, 0x66, 0xCF, 0x11, 0xA6, 0xD9, 0x00, 0xAA, 0x00, 0x62, 0xCE, 0x6C };
        private byte[] ASF_Data_Object =
            {0x36, 0x26, 0xB2, 0x75, 0x8E, 0x66, 0xCF, 0x11, 0xA6, 0xD9, 0x00, 0xAA, 0x00, 0x62, 0xCE, 0x6C };
        private byte[] ASF_Simple_Index_Object =
            { 0x90, 0x08, 0x00, 0x33, 0xB1, 0xE5, 0xCF, 0x11, 0x89, 0xF4, 0x00, 0xA0, 0xC9, 0x03, 0x49, 0xCB };
        private byte[] ASF_Index_Object =
            { 0xD3, 0x29, 0xE2, 0xD6, 0xDA, 0x35, 0xD1, 0x11, 0x90, 0x34, 0x00, 0xA0, 0xC9, 0x03, 0x49, 0xBE };
        private byte[] ASF_Media_Object_Index_Object =
            { 0xF8, 0x03, 0xB1, 0xFE, 0xAD, 0x12, 0x64, 0x4C, 0x84, 0x0F, 0x2A, 0x1D, 0x2F, 0x7A, 0xD4, 0x8C };
        private byte[] ASF_Timecode_Index_Object =
            { 0xD0, 0x3F, 0xB7, 0x3C, 0x4A, 0x0C, 0x03, 0x48, 0x95, 0x3D, 0xED, 0xF7, 0xB6, 0x22, 0x8F, 0x0C };

        //  ASF Header Object GUIDs
        private byte[] ASF_File_Properties_Object =
            { 0xA1, 0xDC, 0xAB, 0x8C, 0x47, 0xA9, 0xCF, 0x11, 0x8E, 0xE4, 0x00, 0xC0, 0x0C, 0x20, 0x53, 0x65 };
        private byte[] ASF_Stream_Properties_Object =
            { 0x91, 0x07, 0xDC, 0xB7, 0xB7, 0xA9, 0xCF, 0x11, 0x8E, 0xE6, 0x00, 0xC0, 0x0C, 0x20, 0x53, 0x65 };
        private byte[] ASF_Header_Extension_Object =
            { 0xB5, 0x03, 0xBF, 0x5F, 0x2E, 0xA9, 0xCF, 0x11, 0x8E, 0xE3, 0x00, 0xC0, 0x0C, 0x20, 0x53, 0x65 };
        private byte[] ASF_Codec_List_Object =
            { 0x40, 0x52, 0xD1, 0x86, 0x1D, 0x31, 0xD0, 0x11, 0xA3, 0xA4, 0x00, 0xA0, 0xC9, 0x03, 0x48, 0xF6 };
        private byte[] ASF_Script_Command_Object =
            { 0x30, 0x1A, 0xFB, 0x1E, 0x62, 0x0B, 0xD0, 0x11, 0xA3, 0x9B, 0x00, 0xA0, 0xC9, 0x03, 0x48, 0xF6 };
        private byte[] ASF_Marker_Object =
            { 0x01, 0xCD, 0x87, 0xF4, 0x51, 0xA9, 0xCF, 0x11, 0x8E, 0xE6, 0x00, 0xC0, 0x0C, 0x20, 0x53, 0x65 };
        private byte[] ASF_Bitrate_Mutual_Exclusion_Object =
            { 0xDC, 0x29, 0xE2, 0xD6, 0xDA, 0x35, 0xD1, 0x11, 0x90, 0x34, 0x00, 0xA0, 0xC9, 0x03, 0x49, 0xBE };
        private byte[] ASF_Error_Correction_Object =
            { 0x35, 0x26, 0xB2, 0x75, 0x8E, 0x66, 0xCF, 0x11, 0xA6, 0xD9, 0x00, 0xAA, 0x00, 0x62, 0xCE, 0x6C };
        private byte[] ASF_Content_Description_Object =
            { 0x33, 0x26, 0xB2, 0x75, 0x8E, 0x66, 0xCF, 0x11, 0xA6, 0xD9, 0x00, 0xAA, 0x00, 0x62, 0xCE, 0x6C };
        private byte[] ASF_Extended_Content_Description_Object =
            { 0x40, 0xA4, 0xD0, 0xD2, 0x07, 0xE3, 0xD2, 0x11, 0x97, 0xF0, 0x00, 0xA0, 0xC9, 0x5E, 0xA8, 0x50};
        private byte[] ASF_Content_Branding_Object =
            { 0xFA, 0xB3, 0x11, 0x22, 0x23, 0xBD, 0xD2, 0x11, 0xB4, 0xB7, 0x00, 0xA0, 0xC9, 0x55, 0xFC, 0x6E };
        private byte[] ASF_Stream_Bitrate_Properties_Object =
            { 0xCE, 0x75, 0xF8, 0x7B, 0x8D, 0x46, 0xD1, 0x11, 0x8D, 0x82, 0x00, 0x60, 0x97, 0xC9, 0xA2, 0xB2 };

        //  Header Extension Object GUIDs
        private byte[] ASF_Extended_Stream_Properties_Object =
            { 0xCB, 0xA5, 0xE6, 0x14, 0x72, 0xC6, 0x32, 0x43, 0x83, 0x99, 0xA9, 0x69, 0x52, 0x06, 0x5B, 0x5A };
        private byte[] ASF_Advanced_Mutual_Exclusion_Objec =
            { 0xCF, 0x49, 0x86, 0xA0, 0x75, 0x47, 0x70, 0x46, 0x8A, 0x16, 0x6E, 0x35, 0x35, 0x75, 0x66, 0xCD };
        private byte[] ASF_Group_Mutual_Exclusion_Object =
            { 0x40, 0x5A, 0x46, 0xD1, 0x79, 0x5A, 0x38, 0x43, 0xB7, 0x1B, 0xE3, 0x6B, 0x8F, 0xD6, 0xC2, 0x49 };
        private byte[] ASF_Stream_Prioritization_Object =
            { 0x5B, 0xD1, 0xFE, 0xD4, 0xD3, 0x88, 0x4F, 0x45, 0x81, 0xF0, 0xED, 0x5C, 0x45, 0x99, 0x9E, 0x24 };
        private byte[] ASF_Bandwidth_Sharing_Object =
            { 0xE6, 0x09, 0x96, 0xA6, 0x7B, 0x51, 0xD2, 0x11, 0xB6, 0xAF, 0x00, 0xC0, 0x4F, 0xD9, 0x08, 0xE9 };
        private byte[] ASF_Language_List_Object =
            { 0xA9, 0x46, 0x43, 0x7C, 0xE0, 0xEF, 0xFC, 0x4B, 0xB2, 0x29, 0x39, 0x3E, 0xDE, 0x41, 0x5C, 0x85 };
        private byte[] ASF_Metadata_Object =
            { 0xEA, 0xCB, 0xF8, 0xC5, 0xAF, 0x5B, 0x77, 0x48, 0x84, 0x67, 0xAA, 0x8C, 0x44, 0xFA, 0x4C, 0xCA };
        private byte[] ASF_Metadata_Library_Object =
            { 0x94, 0x1C, 0x23, 0x44, 0x98, 0x94, 0xD1, 0x49, 0xA1, 0x41, 0x1D, 0x13, 0x4E, 0x45, 0x70, 0x54 };
        private byte[] ASF_Index_Parameters_Object =
            { 0xDF, 0x29, 0xE2, 0xD6, 0xDA, 0x35, 0xD1, 0x11, 0x90, 0x34, 0x00, 0xA0, 0xC9, 0x03, 0x49, 0xBE };
        private byte[] ASF_Media_Object_Index_Parameters_Object =
            { 0xAD, 0x3B, 0x20, 0x6B, 0x11, 0x3F, 0xE4, 0x48, 0xAC, 0xA8, 0xD7, 0x61, 0x3D, 0xE2, 0xCF, 0xA7 };
        private byte[] ASF_Timecode_Index_Parameters_Object =
            { 0x6D, 0x49, 0x5E, 0xF5, 0x97, 0x97, 0x5D, 0x4B, 0x8C, 0x8B, 0x60, 0x4D, 0xFE, 0x9B, 0xFB, 0x24 };
        private byte[] ASF_Compatibility_Object =
            { 0x30, 0x26, 0xB2, 0x75, 0x8E, 0x66, 0xCF, 0x11, 0xA6, 0xD9, 0x00, 0xAA, 0x00, 0x62, 0xCE, 0x6C };
        private byte[] ASF_Advanced_Content_Encryption_Object =
            { 0x33, 0x85, 0x05, 0x43, 0x81, 0x69, 0xE6, 0x49, 0x9B, 0x74, 0xAD, 0x12, 0xCB, 0x86, 0xD5, 0x8C };
        private byte[] ASF_Content_Encryption_Object =
            { 0xFB, 0xB3, 0x11, 0x22, 0x23, 0xBD, 0xD2, 0x11, 0xB4, 0xB7, 0x00, 0xA0, 0xC9, 0x55, 0xFC, 0x6E };
        private byte[] ASF_Extended_Content_Encryption_Object =
            { 0x14, 0xE6, 0x8A, 0x29, 0x22, 0x26, 0x17, 0x4C, 0xB9, 0x35, 0xDA, 0xE0, 0x7E, 0xE9, 0x28, 0x9C };
        private byte[] ASF_Digital_Signature_Object =
            { 0xFC, 0xB3, 0x11, 0x22, 0x23, 0xBD, 0xD2, 0x11, 0xB4, 0xB7, 0x00, 0xA0, 0xC9, 0x55, 0xFC, 0x6E };
        private byte[] ASF_Padding_Object =
            { 0x74, 0xD4, 0x06, 0x18, 0xDF, 0xCA, 0x09, 0x45, 0xA4, 0xBA, 0x9A, 0xAB, 0xCB, 0x96, 0xAA, 0xE8 };

        //  Stream Properties Object Stream Type GUIDs
        private static byte[] ASF_Audio_Media =
            { 0x40, 0x9E, 0x69, 0xF8, 0x4D, 0x5B, 0xCF, 0x11, 0xA8, 0xFD, 0x00, 0x80, 0x5F, 0x5C, 0x44, 0x2B };
        private static byte[] ASF_Video_Media =
            { 0xC0, 0xEF, 0x19, 0xBC, 0x4D, 0x5B, 0xCF, 0x11, 0xA8, 0xFD, 0x00, 0x80, 0x5F, 0x5C, 0x44, 0x2B };
        private static byte[] ASF_Command_Media =
            { 0xC0, 0xCF, 0xDA, 0x59, 0xE6, 0x59, 0xD0, 0x11, 0xA3, 0xAC, 0x00, 0xA0, 0xC9, 0x03, 0x48, 0xF6 };
        private static byte[] ASF_JFIF_Media =
            { 0x00, 0xE1, 0x1B, 0xB6, 0x4E, 0x5B, 0xCF, 0x11, 0xA8, 0xFD, 0x00, 0x80, 0x5F, 0x5C, 0x44, 0x2B };
        private static byte[] ASF_Degradable_JPEG_Media =
            { 0xE0, 0x7D, 0x90, 0x35, 0x15, 0xE4, 0xCF, 0x11, 0xA9, 0x17, 0x00, 0x80, 0x5F, 0x5C, 0x44, 0x2B };
        private static byte[] ASF_File_Transfer_Media =
            { 0x2C, 0x22, 0xBD, 0x91, 0x1C, 0xF2, 0x7A, 0x49, 0x8B, 0x6D, 0x5A, 0xA8, 0x6B, 0xFC, 0x01, 0x85 };
        private static byte[] ASF_Binary_Media =
            { 0xE2, 0x65, 0xFB, 0x3A, 0xEF, 0x47, 0xF2, 0x40, 0xAC, 0x2C, 0x70, 0xA9, 0x0D, 0x71, 0xD3, 0x43 };

        private NameGUID[] mStreamProperties = new NameGUID[] {
            new NameGUID("Audio Media", ASF_Audio_Media),
            new NameGUID("Video Media", ASF_Video_Media),
            new NameGUID("Command Media", ASF_Command_Media),
            new NameGUID("JFIF Media", ASF_JFIF_Media),
            new NameGUID("Degradable JPEG Media", ASF_Degradable_JPEG_Media),
            new NameGUID("File Transfer Media", ASF_File_Transfer_Media),
            new NameGUID("Binary Media", ASF_Binary_Media),
        };

        //  Stream Properties Object Error Correction Type GUIDs
        private static byte[] ASF_No_Error_Correction =
            { 0x00, 0x57, 0xFB, 0x20, 0x55, 0x5B, 0xCF, 0x11, 0xA8, 0xFD, 0x00, 0x80, 0x5F, 0x5C, 0x44, 0x2B };
        private static byte[] ASF_Audio_Spread =
            { 0x50, 0xCD, 0xC3, 0xBF, 0x8F, 0x61, 0xCF, 0x11, 0x8B, 0xB2, 0x00, 0xAA, 0x00, 0xB4, 0xE2, 0x20 };

        private NameGUID[] mStreamPropertiesErrorCorrectionType = new NameGUID[] {
            new NameGUID("No_Error_Correction", ASF_No_Error_Correction),
            new NameGUID("Audio_Spread", ASF_Audio_Spread),
        };

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

        private byte[] mAsfHeader = new byte[30];
        public long mTagSize = 0;
        public List<string> mAsfTagList = new List<string>();
        public Dictionary<string, string> mAsfTags = new Dictionary<string, string>();
        public List<ImageData> mImageData = new List<ImageData>();

        public string mEncording = "";  //   エンコードの種類(おもにPcm(WAVE_FORMAT_PCM)/IeeeFloat(WAVE_FORMAT_IEEE_FLOAT))
        public int mChanneles = 0;      //  チャンネル数 (1=モノラル、2=ステレオなど)
        public int mBitPerSample = 0;   //  量子化ビット数 (たいてい16か32、ときに24か8)
        public int mBlockAlign = 0;     //  ブロック アラインメント (データの最小単位)
        public int mSampleRate = 0;     //  サンプリング周波数 (サンプリングレート)  (1秒あたりのサンプル数)
        public int mSampleBitRate = 0;  //  サンプルビットレート(16-320kbps)（1秒あたりのデータ量）
        public long mDataLength = 0;    //  データサイズ(ファイルサイズ)
        public long mPlayLength;        //  再生時間(msec)


        private FileStream mFileStream;
        private YLib ylib = new YLib();


        public AsfFileTagReader()
        {

        }

        public AsfFileTagReader(string path)
        {
            FileTagRead(path);
        }

        public bool FileTagRead(string path)
        {
            if (!File.Exists(path))
                return false;

            if (Path.GetExtension(path).ToLower().CompareTo(".wma") == 0) {
                //  WMA(ASF)データファイルオープン
                using (mFileStream = new FileStream(path, FileMode.Open, FileAccess.Read)) {
                    mAsfTags.Clear();
                    //  TAGヘッダの読み込み
                    int readBytes = mFileStream.Read(mAsfHeader, 0, mAsfHeader.Length);
                    mTagSize = readBytes;
                    if (readBytes == mAsfHeader.Length) {
                        YLib.binaryDump(mAsfHeader, 0, readBytes, "AsfHeader");
                        if (YLib.ByteComp(mAsfHeader, 0, ASF_Header_Object, 0, 16)) {
                            //  ASF Header Object
                            mAsfTagList.Clear();
                            getWmaTag(mAsfHeader);
                            return true;
                        } else if (YLib.ByteComp(mAsfHeader, 0, ASF_Data_Object, 0, 16)) {
                        }
                        //getFrameHeader();
                    }
                    return true;
                }
            }
            return false;
        }


        /// <summary>
        /// WMAタグ(ASF)の読み込み
        /// http://drang.s4.xrea.com/program/tips/id3tag/tag_wmp.html
        /// http://drang.s4.xrea.com/program/tips/id3tag/wmp/03_asf_top_level_header_object.html
        /// </summary>
        /// <param name="asfHeader"></param>
        private void getWmaTag(byte[] asfHeader)
        {
            mAsfTagList.Add("[ASF Header]");
            int objectSize = (int)YLib.bitConvertLong(asfHeader, 16, 8);    //  ヘッダオブジェクトサイズ
            int objctNumber = (int)YLib.bitConvertLong(asfHeader, 24, 4);   //  ヘッダオブジェクトの数
            byte[] buffer = new byte[objectSize];
            int readBytes = mFileStream.Read(buffer, 0, buffer.Length);
            mTagSize = readBytes;
            int index = 0;
            for (int i = 0; i < objctNumber; i++) {
                YLib.binaryDump(buffer, index, 34, "Header");
                if (YLib.ByteComp(buffer, index, ASF_File_Properties_Object, 0, 16)) {
                    //  ファイル情報
                    index = getFileProperties(index + 16, buffer);
                } else if (YLib.ByteComp(buffer, index, ASF_Stream_Properties_Object, 0, 16)) {
                    //  ストリーム・プロパティ
                    index = getStreamProperties(index + 16, buffer);
                } else if (YLib.ByteComp(buffer, index, ASF_Header_Extension_Object, 0, 16)) {
                    //  拡張データ(中身不明?)
                    index = getHeaderExtension(index + 16, buffer);
                } else if (YLib.ByteComp(buffer, index, ASF_Codec_List_Object, 0, 16)) {
                    //  コーデックリスト
                    index = getCodecList(index + 16, buffer);
                } else if (YLib.ByteComp(buffer, index, ASF_Script_Command_Object, 0, 16)) {
                    //  スクリプトコマンド
                    index += 16;
                    objectSize = (int)YLib.bitConvertLong(buffer, index, 8);
                    System.Diagnostics.Debug.WriteLine("{0} ASF_Script_Command_Object", objectSize);
                    index += objectSize - 16;
                } else if (YLib.ByteComp(buffer, index, ASF_Marker_Object, 0, 16)) {
                    //  マーカー
                    index += 16;
                    objectSize = (int)YLib.bitConvertLong(buffer, index, 8);
                    System.Diagnostics.Debug.WriteLine("{0} ASF_Marker_Object", objectSize);
                    index += objectSize - 16;
                } else if (YLib.ByteComp(buffer, index, ASF_Bitrate_Mutual_Exclusion_Object, 0, 16)) {
                    //  ビットレート相互排他
                    index += 16;
                    objectSize = (int)YLib.bitConvertLong(buffer, index, 8);
                    System.Diagnostics.Debug.WriteLine("{0} ASF_Bitrate_Mutual_Exclusion_Object", objectSize);
                    index += objectSize - 16;
                } else if (YLib.ByteComp(buffer, index, ASF_Error_Correction_Object, 0, 16)) {
                    //  エラー訂正
                    index += 16;
                    objectSize = (int)YLib.bitConvertLong(buffer, index, 8);
                    System.Diagnostics.Debug.WriteLine("{0} ASF_Error_Correction_Object", objectSize);
                    index += objectSize - 16;
                } else if (YLib.ByteComp(buffer, index, ASF_Content_Description_Object, 0, 16)) {
                    //  コンテンツ情報(タイトル、著者、著作権、説明、評価情報など)
                    index = getContentDescription(index + 16, buffer);
                } else if (YLib.ByteComp(buffer, index, ASF_Extended_Content_Description_Object, 0, 16)) {
                    //  拡張コンテンツ情報
                    index = getExtendedContentDescription(index + 16, buffer);
                } else if (YLib.ByteComp(buffer, index, ASF_Content_Branding_Object, 0, 16)) {
                    //  コンテンツブランディング(バナー イメージに関する情報,画像データ)
                    index += 16;
                    objectSize = (int)YLib.bitConvertLong(buffer, index, 8);
                    System.Diagnostics.Debug.WriteLine("{0} ASF_Content_Branding_Object", objectSize);
                    index += objectSize - 16;
                } else if (YLib.ByteComp(buffer, index, ASF_Stream_Bitrate_Properties_Object, 0, 16)) {
                    //  ストリームビットレートプロパティ
                    index = getStreamBitrateProperties(index + 16, buffer);
                } else {
                    YLib.binaryDump(buffer, index, 32, "Header");
                    index += 16;
                    objectSize = (int)YLib.bitConvertLong(buffer, index, 8);
                    System.Diagnostics.Debug.WriteLine("{0} Header", objectSize);
                    index += objectSize;
                }

            }
        }

        /// <summary>
        /// ファイル情報
        /// </summary>
        /// <param name="index"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        private int getFileProperties(int index, byte[] buffer)
        {
            Encoding encoding = Encoding.Unicode;
            int objectSize = (int)YLib.bitConvertLong(buffer, index, 8);
            System.Diagnostics.Debug.WriteLine("{0} ASF_File_Properties_Object", objectSize);
            index += 8;
            //ylib.binaryDump(buffer, index, objectSize, "Object");
            string fileID = YLib.binary2HexString(buffer, index, 16);
            index += 16;
            long fileSize = YLib.bitConvertLong(buffer, index, 8);
            index += 8;
            long createDate = YLib.bitConvertLong(buffer, index, 8);  //  システム時刻(1601/01/01 0.1msec単位)
            index += 8;
            long dataPacketsCount = YLib.bitConvertLong(buffer, index, 8);  //  データパケット数
            index += 8;
            long playDuration = YLib.bitConvertLong(buffer, index, 8);  //  再生時間(100nsec)
            index += 8;
            long sendDuration = YLib.bitConvertLong(buffer, index, 8);  //  送信時間(100nsec)
            index += 8;
            long preroll = YLib.bitConvertLong(buffer, index, 8);       //  再生を開始するまでのバッファリング時間
            index += 8;
            int flags = (int)YLib.bitConvertLong(buffer, index, 4);     //  フラグ
            index += 4;
            int minmumDataPacketSize = (int)YLib.bitConvertLong(buffer, index, 4);  //  最小データパケットサイズ
            index += 4;
            int maximumDataPacketSize = (int)YLib.bitConvertLong(buffer, index, 4);  //  最大データパケットサイズ
            index += 4;
            int maximumBitrate = (int)YLib.bitConvertLong(buffer, index, 4);    //  最大ビットレート
            index += 4;

            mAsfTagList.Add("[File Properties]");
            mAsfTagList.Add("ファイル ID: " + fileID);
            mAsfTagList.Add("ファイルサイズ: " + fileSize.ToString("#,#"));
            mAsfTagList.Add("作成日: " + fileDate2String(createDate));
            mAsfTagList.Add("データパケット数: " + dataPacketsCount);
            if ((flags & 0x01) == 0) {       //  Broadcast Flag == 0 の時有効
                mPlayLength = playDuration / 10000;     //  再生時間msec
                mAsfTagList.Add("再生時間: " + ylib.second2String(playDuration / 10000 / 1000, true));
                mAsfTagList.Add("送信時間: " + ylib.second2String(sendDuration / 10000 / 1000, true));
            }
            mAsfTagList.Add("再生オフセット時間:" + preroll + "msec");
            mAsfTagList.Add("最小データパケットサイズ: " + minmumDataPacketSize.ToString("#,#"));
            mAsfTagList.Add("最大データパケットサイズ: " + maximumDataPacketSize.ToString("#,#"));
            mAsfTagList.Add("最大ビットレート: " + maximumBitrate.ToString("#,#") + " Hz");
            mSampleBitRate = maximumBitrate;     //  ビットレート

            System.Diagnostics.Debug.WriteLine("[{0}] [{1}][{2}][{3}][{4}][{5}][{6}][{7}][{8}][{9}][{10}]",
                fileID, fileSize, fileDate2String(createDate), dataPacketsCount, playDuration, sendDuration,
                preroll, flags, minmumDataPacketSize, maximumDataPacketSize, maximumBitrate);

            return index;
        }

        /// <summary>
        /// ストリームプロパティ
        /// </summary>
        /// <param name="index"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        private int getStreamProperties(int index, byte[] buffer)
        {
            Encoding encoding = Encoding.Unicode;
            long objectSize = YLib.bitConvertLong(buffer, index, 8);
            System.Diagnostics.Debug.WriteLine("{0} ASF_Stream_Properties_Object", objectSize);
            index += 8;
            //ylib.binaryDump(buffer, index, (int)objectSize, "Object");
            byte[] streamTypeGUID = YLib.ByteCopy(buffer, index, 16);
            string streamType = YLib.binary2HexString(buffer, index, 16);   //  ストリームの種類(ASF_Audio_Media)
            index += 16;
            byte[] errorCorrectionTypeGUID = YLib.ByteCopy(buffer, index, 16);
            string errorCorrectionType = YLib.binary2HexString(buffer, index, 16);
            index += 16;
            long timeOffset = YLib.bitConvertLong(buffer, index, 8);
            index += 8;
            int typeSpecificDataLength = (int)YLib.bitConvertLong(buffer, index, 4);
            index += 4;
            int errorCorrectionDataLength = (int)YLib.bitConvertLong(buffer, index, 4);
            index += 4;
            int flags = (int)YLib.bitConvertLong(buffer, index, 2);
            index += 2;
            int reserved = (int)YLib.bitConvertLong(buffer, index, 4);
            index += 4;
            string typeSpecificData = YLib.binary2HexString(buffer, index, typeSpecificDataLength);
            index += typeSpecificDataLength;
            string errorCorrectionData = YLib.binary2HexString(buffer, index, errorCorrectionDataLength);
            index += errorCorrectionDataLength;

            mAsfTagList.Add("[Stream Properties]");
            mAsfTagList.Add("ストリームの種類: " + GUID2String(mStreamProperties, streamTypeGUID));
            mAsfTagList.Add("エラー訂正の種類: " + GUID2String(mStreamPropertiesErrorCorrectionType, streamTypeGUID));
            mAsfTagList.Add("時間オフセット: " + (timeOffset / 10) + " msec");
            mAsfTagList.Add("固有データ長: " + typeSpecificDataLength);
            mAsfTagList.Add("ストリーム番号: " + (flags & 0x3F));
            mAsfTagList.Add("固有データ: " + typeSpecificData);
            mAsfTagList.Add("エラー訂正データ: " + errorCorrectionData);

            System.Diagnostics.Debug.WriteLine("[{0}] [{1}][{2}][{3}][{4}][{5}][{6}]",
                streamType, errorCorrectionType, timeOffset, typeSpecificDataLength,
                errorCorrectionDataLength, flags, reserved);
            System.Diagnostics.Debug.WriteLine("[{0}] [{1}]", typeSpecificData, errorCorrectionData);

            return index;
        }

        /// <summary>
        /// ヘッダー拡張オブジェクト
        /// </summary>
        /// <param name="index"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        private int getHeaderExtension(int index, byte[] buffer)
        {
            Encoding encoding = Encoding.Unicode;
            int objectSize = (int)YLib.bitConvertLong(buffer, index, 8);
            System.Diagnostics.Debug.WriteLine("{0} ASF_Header_Extension_Object", objectSize);
            index += 8;
            //ylib.binaryDump(buffer, index, objectSize, "Object");
            string reservedField1 = YLib.binary2HexString(buffer, index, 16);
            index += 16;
            int reservedField2 = (int)YLib.bitConvertLong(buffer, index, 2);
            index += 2;
            int headerExtensionDataSize = (int)YLib.bitConvertLong(buffer, index, 4);
            index += 4;
            //byte[] headerExtensionData = new byte[headerExtensionDataSize];
            //string headerExtensionData = ylib.binary2HexString(buffer, index, headerExtensionDataSize);
            index += headerExtensionDataSize;

            mAsfTagList.Add("[Header_Extension]");
            mAsfTagList.Add("データサイズ: " + headerExtensionDataSize);
            mDataLength = headerExtensionDataSize;      //  データサイズ

            System.Diagnostics.Debug.WriteLine("[{0}] [{1}][{2}]",
                reservedField1, reservedField2, headerExtensionDataSize);

            return index;
        }

        /// <summary>
        /// コーデックリスト
        /// </summary>
        /// <param name="index"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        private int getCodecList(int index, byte[] buffer)
        {
            Encoding encoding = Encoding.Unicode;
            int objectSize = (int)YLib.bitConvertLong(buffer, index, 8);
            System.Diagnostics.Debug.WriteLine("{0} ASF_Codec_List_Object", objectSize);
            index += 8;
            //ylib.binaryDump(buffer, index, objectSize, "Object");
            string reservedField = YLib.binary2HexString(buffer, index, 16);
            index += 16;
            int codecEntriesCount = (int)YLib.bitConvertLong(buffer, index, 4);
            index += 4;
            mAsfTagList.Add("[Codec List]");
            for (int i = 0; i < codecEntriesCount; i++) {
                int type = (int)YLib.bitConvertLong(buffer, index, 2);
                index += 2;
                int codecNameLength = (int)YLib.bitConvertLong(buffer, index, 2) * 2;
                index += 2;
                string codecName = "";
                if (0 < codecNameLength)
                    codecName = encoding.GetString(buffer, index, codecNameLength - 2);
                index += codecNameLength;
                int codecDescriptionLength = (int)YLib.bitConvertLong(buffer, index, 2) * 2;
                index += 2;
                string codecDescription = "";
                if (0 < codecDescriptionLength)
                    codecDescription = encoding.GetString(buffer, index, codecDescriptionLength - 2);
                index += codecDescriptionLength;
                int codecInformationLength = (int)YLib.bitConvertLong(buffer, index, 2);
                index += 2;
                string codecInformation = YLib.binary2HexString(buffer, index, codecInformationLength);
                index += codecInformationLength;

                //  データをタグリストに登録
                mAsfTagList.Add((type==1?"ビデオ":(type==2?"オーディオ":"不明")) + " " + codecName + ": " + codecDescription);
                //  CODEC情報(サンプリング周波数、サンプルビットレート、チャンネル数)の取得
                if (type == 2) {
                    //  オーディオデータの時
                    mChanneles = 1;
                    string[] datas = codecDescription.Split(',');
                    for (i = 0; i < datas.Length; i++) {
                        if (0 < datas[i].IndexOf("kHz")) {
                            mSampleRate = (int)ylib.string2double(datas[i]) * 1000;
                        } else if (0 < datas[i].IndexOf("kbps")) {
                            mSampleBitRate = (int)ylib.string2double(datas[i]) * 1000;
                        } else if (0 < datas[i].IndexOf("stereo")) {
                            mChanneles = 2;
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine("{0} [{1}][{2}][{3}][{4}][{5}][{6}][{7}]",
                    i, type, codecNameLength, codecName, codecDescriptionLength, codecDescription, codecInformationLength, codecInformation);
            }
            return index;
        }

        /// <summary>
        /// ファイルとその内容を記述した既知のデータを記録
        /// タイトル、著者、著作権、説明、評価情報などの標準的な書誌情報を格納
        /// </summary>
        /// <param name="index"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        private int getContentDescription(int index, byte[] buffer)
        {
            Encoding encoding = Encoding.Unicode;
            int objectSize = (int)YLib.bitConvertLong(buffer, index, 8);
            System.Diagnostics.Debug.WriteLine("{0} ASF_Content_Description_Object", objectSize);
            index += 8;
            //ylib.binaryDump(buffer, index, objectSize, "Object");
            int titleLength = (int)YLib.bitConvertLong(buffer, index, 2);
            index += 2;
            int autherLength = (int)YLib.bitConvertLong(buffer, index, 2);
            index += 2;
            int copyrightLength = (int)YLib.bitConvertLong(buffer, index, 2);
            index += 2;
            int descriptionLength = (int)YLib.bitConvertLong(buffer, index, 2);
            index += 2;
            int ratingLength = (int)YLib.bitConvertLong(buffer, index, 2);
            index += 2;
            string title = "";
            if (0 < titleLength)
                title = encoding.GetString(buffer, index, titleLength - 2);
            index += titleLength;
            string auther = "";
            if (0 < autherLength)
                auther = encoding.GetString(buffer, index, autherLength - 2);
            index += autherLength;
            string copyright = "";
            if (0 < copyrightLength)
                copyright = encoding.GetString(buffer, index, copyrightLength - 2);
            index += copyrightLength;
            string description = "";
            if (0 < descriptionLength)
                description = encoding.GetString(buffer, index, descriptionLength - 2);
            index += descriptionLength;
            string rating = "";
            if (0 < ratingLength)
                rating = encoding.GetString(buffer, index, ratingLength - 2);
            index += ratingLength;

            //  データをタグリストに登録
            mAsfTagList.Add("[Content Description]");
            mAsfTagList.Add("タイトル: " + title);
            mAsfTagList.Add("作成者　: " + auther);
            mAsfTagList.Add("著作権　: " + copyright);
            mAsfTagList.Add("説明　　: " + description);
            mAsfTagList.Add("評価情報: " + rating);

            //  データをタグDictionaryに登録
            setTagData("TITLE", title);
            setTagData("ARTIST", auther);
            setTagData("COPYRIGHT", copyright);
            setTagData("COMMENT", description);
            setTagData("RATING", description);

            System.Diagnostics.Debug.WriteLine("{0}:[{1}][{2}][{3}][{4}][{5}]",
                objectSize, title, auther, copyright, description, rating);
            return index;
        }

        /// <summary>
        /// タイトル、作成者、著作権、説明、評価情報などの標準的な書誌情報を超えたファイルとその内容を記述したデータを記録
        /// 情報はファイル全体に関連し
        /// 名称と値のペアのメタファを使用
        /// 名称(WM/AlbumArtist,WM/AlbumTitle ...)についてはMicrosoft Docsのattribute listに説明がある
        /// https://docs.microsoft.com/en-us/windows/win32/wmformat/attribute-list
        /// </summary>
        /// <param name="index"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        private int getExtendedContentDescription(int index, byte[] buffer)
        {
            Encoding encoding = Encoding.Unicode;
            int objectSize = (int)YLib.bitConvertLong(buffer, index, 8);
            System.Diagnostics.Debug.WriteLine("{0} ASF_Extended_Content_Description_Object", objectSize);
            index += 8;
            //ylib.binaryDump(buffer, index, objectSize, "Object");
            int contentDescriptionCount = (int)YLib.bitConvertLong(buffer, index, 2);
            index += 2;
            System.Diagnostics.Debug.WriteLine("{0}:[{1}]",
                objectSize, contentDescriptionCount);
            mAsfTagList.Add("[Extended Content]");
            for (int j = 0; j < contentDescriptionCount; j++) {
                int descriptorNameLength = (int)YLib.bitConvertLong(buffer, index, 2);
                index += 2;
                string descriptorName = "";
                if (0 < descriptorNameLength)
                    descriptorName = encoding.GetString(buffer, index, descriptorNameLength - 2);
                index += descriptorNameLength;
                int descriptorValueDateType = (int)YLib.bitConvertLong(buffer, index, 2);
                index += 2;
                int descriptorValueLength = (int)YLib.bitConvertLong(buffer, index, 2);
                index += 2;
                string descriptorValue = "";
                byte[] descriptorValueArray = new byte[0];
                if (descriptorValueDateType == 0) {
                    //  Unicodestrinigs
                    descriptorValue = encoding.GetString(buffer, index, descriptorValueLength - 2);
                } else if (descriptorValueDateType == 1) {
                    //  BYTE array
                    descriptorValueArray = YLib.ByteCopy(buffer, index, descriptorValueLength);
                    descriptorValue = YLib.binary2HexString(descriptorValueArray, 0, Math.Min(descriptorValueArray.Length, 16));
                } else if (descriptorValueDateType == 2) {
                    //  BOOL
                    int descriptor = (int)YLib.bitConvertLong(buffer, index, 4);
                    descriptorValue = (descriptor == 0 ? "false" : "true");
                } else if (descriptorValueDateType == 3) {
                    //  DWORD
                    int descriptor = (int)YLib.bitConvertLong(buffer, index, 4);
                    descriptorValue = descriptor.ToString();
                } else if (descriptorValueDateType == 4) {
                    //  QWORD
                    long descriptor = YLib.bitConvertLong(buffer, index, 8);
                    descriptorValue = descriptor.ToString();
                } else if (descriptorValueDateType == 5) {
                    //  WORD
                    int descriptor = (int)YLib.bitConvertLong(buffer, index, 2);
                } else {
                    descriptorValue = "";
                }
                index += descriptorValueLength;

                //  データをタグDictionaryに登録
                setTagData(descriptorName, descriptorValue);

                //  データをタグリストに登録
                mAsfTagList.Add(descriptorName + ": " + descriptorValue);
                if (descriptorName.CompareTo("WM/Picture") == 0) {
                    if (0 < descriptorValueArray.Length) {
                        WmPicture picture = new WmPicture(descriptorValueArray);
                        //  画像データをImageDataクラスに入れる
                        ImageData imageData = new ImageData();
                        imageData.Description = picture.Description;
                        imageData.FileExt = "";
                        imageData.ImageType = picture.Mime;
                        imageData.PictureType = mPictureType[picture.Type];
                        imageData.PictureData = picture.Data; 
                        //  画像リストに登録
                        mImageData.Add(imageData);
                        
                        mAsfTagList.Add(" PictureType: " + mPictureType[picture.Type]);
                        mAsfTagList.Add(" MIMEtype: " + picture.Mime);
                        mAsfTagList.Add(" Description: " + picture.Description);
                    }
                }

                System.Diagnostics.Debug.WriteLine("{0}:[{1}][{2}][{3}][{4}]",
                    descriptorNameLength, descriptorName, descriptorValueDateType, descriptorValueLength, descriptorValue);
            }
            return index;
        }

        /// <summary>
        /// ストリームビットレートプロパティ
        /// ^平均ビットレートの取得
        /// </summary>
        /// <param name="index"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        private int getStreamBitrateProperties(int index, byte[] buffer)
        {
            Encoding encoding = Encoding.Unicode;
            int objectSize = (int)YLib.bitConvertLong(buffer, index, 8);
            System.Diagnostics.Debug.WriteLine("{0} ASF_Stream_Bitrate_Properties_Object", objectSize);
            index += 8;
            //ylib.binaryDump(buffer, index, objectSize, "Object");
            int bitrateRecordsCount = (int)YLib.bitConvertLong(buffer, index, 2);
            index += 2;
            System.Diagnostics.Debug.WriteLine("[{0}]", bitrateRecordsCount);
            mAsfTagList.Add("[Stream Bitrate Properties]");
            for (int i = 0; i < bitrateRecordsCount; i++) {
                int flags = (int)YLib.bitConvertLong(buffer, index, 2);
                index += 2;
                int averageBitrate = (int)YLib.bitConvertLong(buffer, index, 4);
                index += 4;

                mAsfTagList.Add("平均ビットレート: [" + (flags & 0x7F) + "] " + averageBitrate);
                mSampleBitRate = averageBitrate;     //  ピッレート

                System.Diagnostics.Debug.WriteLine("{0}[{1}][{2}]", i, flags, averageBitrate);
            }

            return index;
        }

        /// <summary>
        /// システム時刻を文字列に変換
        /// 1601/01/01からの100 n sec単位
        /// </summary>
        /// <param name="fileDate"></param>
        /// <returns></returns>
        private string fileDate2String(long fileDate)
        {
            DateTime dt = new DateTime(1600, 1, 1);
            dt += new TimeSpan(fileDate);
            return dt.ToString();
        }

        /// <summary>
        /// 音楽タグに登録
        /// </summary>
        /// <param name="key"></param>
        /// <param name="data"></param>
        private void setTagData(string key, string data)
        {
            if (mAsfTags.ContainsKey(key))
                mAsfTags[key] += "," + data;
            else
                mAsfTags.Add(key, data);
        }

        /// <summary>
        /// GUIDを検索して名称を返す
        /// </summary>
        /// <param name="nameGuid">GUIDの配列</param>
        /// <param name="guid">名称</param>
        /// <returns></returns>
        private string GUID2String(NameGUID[] nameGuid, byte[] guid)
        {
            for (int i = 0; i < nameGuid.Length; i++) {
                if (YLib.ByteComp(guid, nameGuid[i].GUID))
                    return nameGuid[i].Name;
            }
            return "";
        }
    }

    /// <summary>
    /// WM/Picture の内容を表します。
    /// https://akabeko.me/blog/2010/10/wm-picture-structure/
    /// </summary>
    class WmPicture
    {
        /// <summary>
        /// インスタンスを初期化します。
        /// </summary>
        /// <param name="bytes">WM/Picture の内容を格納したバイト配列。</param>
        public WmPicture(byte[] bytes)
        {
            // 種別とサイズ
            this.Type = bytes[0];
            var size = BitConverter.ToUInt32(bytes, 1);

            // MIME と説明文
            var position = 5;
            this.Mime = this.ReadString(bytes, ref position);
            this.Description = this.ReadString(bytes, ref position);

            // 画像
            this.Data = new byte[size];
            Array.Copy(bytes, position, this.Data, 0, size);
        }

        /// <summary>
        /// バイト配列の指定位置から、UTF16LE 文字列を NULL 終端まで読み込みます。
        /// </summary>
        /// <param name="bytes">バイト配列</param>
        /// <param name="position">読み込み開始位置。読み終えた後は NULL 終端の直後を指します。</param>
        /// <returns>読み込んだ文字列。</returns>
        private string ReadString(byte[] bytes, ref int position)
        {
            // 終端の検索 ( 終端直前の文字が後続バイトにゼロを使用する場合も考慮する )
            var index = this.BytesIndexOf(bytes, StringTerminateBytes, position);
            if (bytes[index + StringTerminateBytes.Length] == 0) { ++index; }

            var str = Encoding.Unicode.GetString(bytes, position, index - position);
            position = index + StringTerminateBytes.Length;

            return str;
        }

        /// <summary>
        /// バイト配列内から指定されたバイト配列を探します。
        /// </summary>
        /// <param name="bytes">バイト配列。</param>
        /// <param name="token">検索するバイト配列。</param>
        /// <param name="index">検索を開始する位置。</param>
        /// <returns>バイト配列の見つかった位置を示すインデックス。未検出の場合は -1。</returns>
        /// <exception cref="ArgumentNullException">bytes または token が null 参照です。</exception>
        private int BytesIndexOf(byte[] bytes, byte[] token, int index)
        {
            // 検索対象なし
            if (bytes == null || token == null) { throw new ArgumentNullException("\"bytes\" or \"token\" is null."); }

            // 検索できるデータ指定ではないので未検出として扱う
            if (bytes.Length == 0 || token.Length == 0 || token.Length > (bytes.Length - index)) { return -1; }

            var max = bytes.Length - (token.Length + 1);
            for (var i = index; i < max; ++i) {
                for (var j = 0; j < token.Length; ++j) {
                    if (bytes[i + j] != token[j]) {
                        break;
                    } else if (j == token.Length - 1) {
                        return i;
                    }
                }
            }

            return -1;
        }

        /// <summary>
        /// 画像データを示すバイト配列を取得します。
        /// </summary>
        public byte[] Data { get; private set; }

        /// <summary>
        /// 画像の説明を取得します。
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// 画像の MIME ( Multipurpose Internet Mail Extension ) 情報を取得します。
        /// </summary>
        public string Mime { get; private set; }

        /// <summary>
        /// 画像の種別を取得します。
        /// </summary>
        public int Type { get; private set; }

        /// <summary>
        /// 文字列の終端を表すバイト配列。
        /// </summary>
        private static readonly byte[] StringTerminateBytes = { 0, 0 };
    }
}
