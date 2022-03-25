using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using WpfLib;

namespace AudioLib
{
    /// <summary>
    ///     /// MP3のタグデータの取得

    /// </summary>
    public class FileTagReader
    {
        //  画像データの種別
        private  readonly Dictionary<int, string> mPictureType = new Dictionary<int, string>() {
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

        //  ID3v1 ジャンルデータ
        private readonly Dictionary<int, string> mGenreListV1 = new Dictionary<int, string>() {
            {0x00, "Blues"},
            {0x01, "ClassicRock"},
            {0x02, "Country"},
            {0x03, "Dance"},
            {0x04, "Disco"},
            {0x05, "Funk"},
            {0x06, "Grunge"},
            {0x07, "Hip-Hop"},
            {0x08, "Jazz"},
            {0x09, "Metal"},
            {0x0a, "NewAge"},
            {0x0b, "Oldies"},
            {0x0c, "Other"},
            {0x0d, "Pop"},
            {0x0e, "R&B"},
            {0x0f, "Rap"},
            {0x10, "Reggae"},
            {0x11, "Rock"},
            {0x12, "Techno"},
            {0x13, "Industrial"},
            {0x14, "Alternative"},
            {0x15, "Ska"},
            {0x16, "DeathMetal"},
            {0x17, "Pranks"},
            {0x18, "Soundtrack"},
            {0x19, "Euro-Techno"},
            {0x1a, "Ambient"},
            {0x1b, "Trip-Hop"},
            {0x1c, "Vocal"},
            {0x1d, "Jazz+Funk"},
            {0x1e, "Fusion"},
            {0x1f, "Trance"},
            {0x20, "Classical"},
            {0x21, "Instrumental"},
            {0x22, "Acid"},
            {0x23, "House"},
            {0x24, "Game"},
            {0x25, "SoundClip"},
            {0x26, "Gospel"},
            {0x27, "Noise"},
            {0x28, "Alt.Rock"},
            {0x29, "Bass"},
            {0x2a, "Soul"},
            {0x2b, "Punk"},
            {0x2c, "Space"},
            {0x2d, "Meditative"},
            {0x2e, "InstrumentalPop"},
            {0x2f, "InstrumentalRock"},
            {0x30, "Ethnic"},
            {0x31, "Gothic"},
            {0x32, "Darkwave"},
            {0x33, "Techno-Industrial"},
            {0x34, "Electronic"},
            {0x35, "Pop-Folk"},
            {0x36, "Eurodance"},
            {0x37, "Dream"},
            {0x38, "SouthernRock"},
            {0x39, "Comedy"},
            {0x3a, "Cult"},
            {0x3b, "Gangsta"},
            {0x3c, "Top40"},
            {0x3d, "ChristianRap"},
            {0x3e, "Pop/Funk"},
            {0x3f, "Jungle"},
            {0x40, "NativeAmerican"},
            {0x41, "Cabaret"},
            {0x42, "NewWave"},
            {0x43, "Psychadelic"},
            {0x44, "Rave"},
            {0x45, "Showtunes"},
            {0x46, "Trailer"},
            {0x47, "Lo-Fi"},
            {0x48, "Tribal"},
            {0x49, "AcidPunk"},
            {0x4a, "AcidJazz"},
            {0x4b, "Polka"},
            {0x4c, "Retro"},
            {0x4d, "Musical"},
            {0x4e, "Rock&Roll"},
            {0x4f, "HardRock"},
            {0x50, "Folk"},
            {0x51, "Folk/Rock"},
            {0x52, "NationalFolk"},
            {0x53, "Swing"},
            {0x54, "Fusion"},
            {0x55, "Bebob"},
            {0x56, "Latin"},
            {0x57, "Revival"},
            {0x58, "Celtic"},
            {0x59, "Bluegrass"},
            {0x5a, "Avantgarde"},
            {0x5b, "GothicRock"},
            {0x5c, "ProgressiveRock"},
            {0x5d, "PsychedelicRock"},
            {0x5e, "SymphonicRock"},
            {0x5f, "SlowRock"},
            {0x60, "BigBand"},
            {0x61, "Chorus"},
            {0x62, "EasyListening"},
            {0x63, "Acoustic"},
            {0x64, "Humour"},
            {0x65, "Speech"},
            {0x66, "Chanson"},
            {0x67, "Opera"},
            {0x68, "ChamberMusic"},
            {0x69, "Sonata"},
            {0x6a, "Symphony"},
            {0x6b, "BootyBass"},
            {0x6c, "Primus"},
            {0x6d, "PornGroove"},
            {0x6e, "Satire"},
            {0x6f, "SlowJam"},
            {0x70, "Club"},
            {0x71, "Tango"},
            {0x72, "Samba"},
            {0x73, "Folklore"},
            {0x74, "Ballad"},
            {0x75, "Power Ballad"},
            {0x76, "Rhytmic Soul"},
            {0x77, "Freestyle"},
            {0x78, "Duet"},
            {0x79, "Punk Rock"},
            {0x7a, "Drum Solo"},
            {0x7b, "Acapella"},
            {0x7c, "Euro-House"},
            {0x7d, "Dance Hall"},
            {0x7e, "Goa"},
            {0x7f, "Drum & Bass"},
            {0x80, "Club-House"},
            {0x90, "Trash Metal"},
            {0x81, "Hardcore"},
            {0x91, "Anime"},
            {0x82, "Terror"},
            {0x92, "JPop"},
            {0x83, "Indie"},
            {0x93, "SynthPop"},
            {0x84, "BritPop"},
            {0x85, "Negerpunk"},
            {0x86, "Polsk Punk"},
            {0x87, "Beat"},
            {0x88, "Christian Gangsta Rap"},
            {0x89, "Heavy Metal"},
            {0x8a, "Black Metal"},
            {0x8b, "Crossover"},
            {0x8c, "Contemporary Christian"},
            {0x8d, "Christian Rock"},
            {0x8e, "Merengue"},
            {0x8f, "Salsa"},
            {0xf0, "Sacred"},
            {0xf1, "Northern Europe"},
            {0xf2, "Irish & Scottish"},
            {0xf3, "Scotland"},
            {0xf4, "Ethnic Europe"},
            {0xf5, "Enka"},
            {0xf6, "Children's Song"},
            {0xf7, "空き"},
            {0xf8, "Heavy Rock(J)"},
            {0xf9, "Doom Rock(J)"},
            {0xfa, "J-POP(J)"},
            {0xfb, "Seiyu(J)"},
            {0xfc, "Tecno Ambient(J)"},
            {0xfd, "Moemoe(J)"},
            {0xfe, "Tokusatsu(J)"},
            {0xff, "Anime(J)"},
        };

        //  ID3v2 ジャンルデータ
        private readonly Dictionary<int, string> mGenreListV220 = new Dictionary<int, string>() {
            {0, "Blues"},
            {1, "Classic Rock"},
            {2, "Country"},
            {3, "Dance"},
            {4, "Disco"},
            {5, "Funk"},
            {6, "Grunge"},
            {7, "Hip-Hop"},
            {8, "Jazz"},
            {9, "Metal"},
            {10, "New Age"},
            {11, "Oldies"},
            {12, "Other"},
            {13, "Pop"},
            {14, "R&B"},
            {15, "Rap"},
            {16, "Reggae"},
            {17, "Rock"},
            {18, "Techno"},
            {19, "Industrial"},
            {20, "Alternative"},
            {21, "Ska"},
            {22, "Death Metal"},
            {23, "Pranks"},
            {24, "Soundtrack"},
            {25, "Euro-Techno"},
            {26, "Ambient"},
            {27, "Trip-Hop"},
            {28, "Vocal"},
            {29, "Jazz+Funk"},
            {30, "Fusion"},
            {31, "Trance"},
            {32, "Classical"},
            {33, "Instrumental"},
            {34, "Acid"},
            {35, "House"},
            {36, "Game"},
            {37, "Sound Clip"},
            {38, "Gospel"},
            {39, "Noise"},
            {40, "AlternRock"},
            {41, "Bass"},
            {42, "Soul"},
            {43, "Punk"},
            {44, "Space"},
            {45, "Meditative"},
            {46, "Instrumental Pop"},
            {47, "Instrumental Rock"},
            {48, "Ethnic"},
            {49, "Gothic"},
            {50, "Darkwave"},
            {51, "Techno-Industrial"},
            {52, "Electronic"},
            {53, "Pop-Folk"},
            {54, "Eurodance"},
            {55, "Dream"},
            {56, "Southern Rock"},
            {57, "Comedy"},
            {58, "Cult"},
            {59, "Gangsta"},
            {60, "Top 40"},
            {61, "Christian Rap"},
            {62, "Pop/Funk"},
            {63, "Jungle"},
            {64, "Native American"},
            {65, "Cabaret"},
            {66, "New Wave"},
            {67, "Psychadelic"},
            {68, "Rave"},
            {69, "Showtunes"},
            {70, "Trailer"},
            {71, "Lo-Fi"},
            {72, "Tribal"},
            {73, "Acid Punk"},
            {74, "Acid Jazz"},
            {75, "Polka"},
            {76, "Retro"},
            {77, "Musical"},
            {78, "Rock & Roll"},
            {79, "Hard Rock"},
            {80, "Folk"},
            {81, "Folk-Rock"},
            {82, "National Folk"},
            {83, "Swing"},
            {84, "Fast Fusion"},
            {85, "Bebob"},
            {86, "Latin"},
            {87, "Revival"},
            {88, "Celtic"},
            {89, "Bluegrass"},
            {90, "Avantgarde"},
            {91, "Gothic Rock"},
            {92, "Progressive Rock"},
            {93, "Psychedelic Rock"},
            {94, "Symphonic Rock"},
            {95, "Slow Rock"},
            {96, "Big Band"},
            {97, "Chorus"},
            {98, "Easy Listening"},
            {99, "Acoustic"},
            {100, "Humour"},
            {101, "Speech"},
            {102, "Chanson"},
            {103, "Opera"},
            {104, "Chamber Music"},
            {105, "Sonata"},
            {106, "Symphony"},
            {107, "Booty Bass"},
            {108, "Primus"},
            {109, "Porn Groove"},
            {110, "Satire"},
            {111, "Slow Jam"},
            {112, "Club"},
            {113, "Tango"},
            {114, "Samba"},
            {115, "Folklore"},
            {116, "Ballad"},
            {117, "Power Ballad"},
            {118, "Rhythmic Soul"},
            {119, "Freestyle"},
            {120, "Duet"},
            {121, "Punk Rock"},
            {122, "Drum Solo"},
            {123, "A capella"},
            {124, "Euro-House"},
            {125, "Dance Hall"},
        };

        //  ID3v2.2.0 tag description
        private readonly Dictionary<string, string> mTagV220Name = new Dictionary<string, string>() {
            {"BUF","Recommended buffer size"},
            {"CNT","Play counter"},
            {"COM","Comments"},
            {"CRA","Audio encryption"},
            {"CRM","Encrypted meta frame"},
            {"ETC","Event timing codes"},
            {"EQU","Equalization"},
            {"GEO","General encapsulated object"},
            {"IPL","Involved people list"},
            {"LNK","Linked information"},
            {"MCI","Music CD Identifier"},
            {"MLL","MPEG location lookup table"},
            {"PIC","Attached picture"},
            {"POP","Popularimeter"},
            {"REV","Reverb"},
            {"RVA","Relative volume adjustment"},
            {"SLT","Synchronized lyric/text"},
            {"STC","Synced tempo codes"},
            {"TAL","Album/Movie/Show title"},
            {"TBP","BPM (Beats Per Minute)"},
            {"TCM","Composer"},
            {"TCO","Content type"},
            {"TCR","Copyright message"},
            {"TDA","Date"},
            {"TDY","Playlist delay"},
            {"TEN","Encoded by"},
            {"TFT","File type"},
            {"TIM","Time"},
            {"TKE","Initial key"},
            {"TLA","Language(s)"},
            {"TLE","Length"},
            {"TMT","Media type"},
            {"TOA","Original artist(s)/performer(s)"},
            {"TOF","Original filename"},
            {"TOL","Original Lyricist(s)/text writer(s)"},
            {"TOR","Original release year"},
            {"TOT","Original album/Movie/Show title"},
            {"TP1","Lead artist(s)/Lead performer(s)/Soloist(s)/Performing group"},
            {"TP2","Band/Orchestra/Accompaniment"},
            {"TP3","Conductor/Performer refinement"},
            {"TP4","Interpreted, remixed, or otherwise modified by"},
            {"TPA","Part of a set"},
            {"TPB","Publisher"},
            {"TRC","ISRC (International Standard Recording Code)"},
            {"TRD","Recording dates"},
            {"TRK","Track number/Position in set"},
            {"TSI","Size"},
            {"TSS","Software/hardware and settings used for encoding"},
            {"TT1","Content group description"},
            {"TT2","Title/Songname/Content description"},
            {"TT3","Subtitle/Description refinement"},
            {"TXT","Lyricist/text writer"},
            {"TXX","User defined text information frame"},
            {"TYE","Year"},
            {"UFI","Unique file identifier"},
            {"ULT","Unsychronized lyric/text transcription"},
            {"WAF","Official audio file webpage"},
            {"WAR","Official artist/performer webpage"},
            {"WAS","Official audio source webpage"},
            {"WCM","Commercial information"},
            {"WCP","Copyright/Legal information"},
            {"WPB","Publishers official webpage"},
            {"WXX","User defined URL link frame"},
        };
        //  ID3v2.3.0 tag description
        private readonly Dictionary<string, string> mTagV230Name = new Dictionary<string, string>() {
            {"AENC","Audio encryption"},
            {"APIC","Attached picture"},
            {"COMM","Comments"},
            {"COMR","Commercial frame"},
            {"ENCR","Encryption method registration"},
            {"EQUA","Equalization"},
            {"ETCO","Event timing codes"},
            {"GEOB","General encapsulated object"},
            {"GRID","Group identification registration"},
            {"IPLS","Involved people list"},
            {"LINK","Linked information"},
            {"MCDI","Music CD identifier"},
            {"MLLT","MPEG location lookup table"},
            {"OWNE","Ownership frame"},
            {"PRIV","Private frame"},
            {"PCNT","Play counter"},
            {"POPM","Popularimeter"},
            {"POSS","Position synchronisation frame"},
            {"RBUF","Recommended buffer size"},
            {"RVAD","Relative volume adjustment"},
            {"RVRB","Reverb"},
            {"SYLT","Synchronized lyric/text"},
            {"SYTC","Synchronized tempo codes"},
            {"TALB","Album/Movie/Show title"},
            {"TBPM","BPM (beats per minute)"},
            {"TCOM","Composer"},
            {"TCON","Content type"},
            {"TCOP","Copyright message"},
            {"TDAT","Date"},
            {"TDLY","Playlist delay"},
            {"TENC","Encoded by"},
            {"TEXT","Lyricist/Text writer"},
            {"TFLT","File type"},
            {"TIME","Time"},
            {"TIT1","Content group description"},
            {"TIT2","Title/songname/content description"},
            {"TIT3","Subtitle/Description refinement"},
            {"TKEY","Initial key"},
            {"TLAN","Language(s)"},
            {"TLEN","Length"},
            {"TMED","Media type"},
            {"TOAL","Original album/movie/show title"},
            {"TOFN","Original filename"},
            {"TOLY","Original lyricist(s)/text writer(s)"},
            {"TOPE","Original artist(s)/performer(s)"},
            {"TORY","Original release year"},
            {"TOWN","File owner/licensee"},
            {"TPE1","Lead performer(s)/Soloist(s)"},
            {"TPE2","Band/orchestra/accompaniment"},
            {"TPE3","Conductor/performer refinement"},
            {"TPE4","Interpreted, remixed, or otherwise modified by"},
            {"TPOS","Part of a set"},
            {"TPUB","Publisher"},
            {"TRCK","Track number/Position in set"},
            {"TRDA","Recording dates"},
            {"TRSN","Internet radio station name"},
            {"TRSO","Internet radio station owner"},
            {"TSIZ","Size"},
            {"TSRC","ISRC (international standard recording code)"},
            {"TSSE","Software/Hardware and settings used for encoding"},
            {"TYER","Year"},
            {"TXXX","User defined text information frame"},
            {"UFID","Unique file identifier"},
            {"USER","Terms of use"},
            {"USLT","Unsychronized lyric/text transcription"},
            {"WCOM","Commercial information"},
            {"WCOP","Copyright/Legal information"},
            {"WOAF","Official audio file webpage"},
            {"WOAR","Official artist/performer webpage"},
            {"WOAS","Official audio source webpage"},
            {"WORS","Official internet radio station homepage"},
            {"WPAY","Payment"},
            {"WPUB","Publishers official webpage"},
            {"WXXX","User defined URL link frame"},
        };

        //  タグ名称変換テーブル
        private readonly Dictionary<string, string[]> mTagConverte = new Dictionary<string, string[]>() {
            //  KEY                      ID#V1     ID3V2.2  V2.3    FLAC             ASF(WMA)
            {"TITLE",       new string[]{"TITLE",   "TT2", "TIT2", "TITLE",          "TITLE" } },
            {"ALBUM",       new string[]{"ALBUM",   "TAL", "TALB", "ALBUM",          "WM/AlbumTitle" } },
            {"ARTIST",      new string[]{"ARTIST",  "TP1", "TPE1", "DISPLAY ARTIST", "ARTIST" } },
            {"ALBUMARTIST", new string[]{"ARTIST",  "TP2", "TPE2", "ALBUMARTIST",    "WM/AlbumArtist" } },
            {"YEAR",        new string[]{"YEAR",    "TYE", "TYER", "DATE",           "WM/Year" } },
            {"GENRE",       new string[]{"GENRE",   "TCO", "TCON", "GENRE",          "WM/GenreID" } },
            {"COMMENT",     new string[]{"COMMENT", "COM", "COMM", "COMMENT",        "COMMENT" } },
            {"TRACKNUMBER", new string[]{"",        "TRK", "TRCK", "TRACKNUMBER",    "WM/TrackNumber" } },
            {"DISCNUMBER",  new string[]{"",        "TPA", "TPOS", "",               "" } },

        };
        private string[] mVerTitle = { "ID3V1","ID3V2.2.0", "ID3V2.3.0", "FLAC","ASF" };
        
        private FileStream mFileStream;
        private byte[] mTagHeader = new byte[3];
        private byte[] mTagData;
        private string mVer = "";
        private long mTagSize = 0;
        private long mFrameSize;
        private int mFrameIdSize = 4;
        private Dictionary<string, string> mID3Tags = new Dictionary<string, string>();

        private List<ImageData> mImageData = new List<ImageData>();
        private AsfFileTagReader mAsfFileTagReader;
        private FlacFileTagReader mFlacFileTagReader;

        private YLib ylib = new YLib();

        public FileTagReader( )
        {

        }

        /// <summary>
        /// MP3/FLACのファイルタグの読み込み
        /// </summary>
        /// <param name="path">ファイルパス</param>
        public FileTagReader(string path)
        {
            FileTagRead(path);
        }

        /// <summary>
        /// MP3/FLACのファイルタグの読み込み
        /// </summary>
        /// <param name="path">ファイル名パス</param>
        public void FileTagRead(string path)
        {
            if (!File.Exists(path))
                return;

            if (Path.GetExtension(path).ToLower().CompareTo(".flac") == 0) {
                //  FLACデータファイルのタグ情報取得
                mVer = "FLAC";
                mFlacFileTagReader = new FlacFileTagReader(path);
            } else if (Path.GetExtension(path).ToLower().CompareTo(".wma") == 0) {
                //  WMA(ASF)データファイルのタグ情報取得
                mVer = "ASF";
                mAsfFileTagReader = new AsfFileTagReader(path);
            } else {
                //  MP3,WAVEファイルのID3タグ情報の取得
                using (mFileStream = new FileStream(path, FileMode.Open, FileAccess.Read)) {
                    int readBytes = mFileStream.Read(mTagHeader, 0, mTagHeader.Length);
                    if (readBytes == mTagHeader.Length) {
                        string tagHeadText = Encoding.ASCII.GetString(mTagHeader);
                        if (tagHeadText.CompareTo("TAG") == 0) {
                            getId3V1Tag();      //  ID3V1
                        } else if (tagHeadText.CompareTo("ID3") == 0) {
                            getId3V2Tag();      //  ID3V2
                            convertGenreV220(); //  括弧付き数値のジャンルを文字列に置き換える
                        }
                        //getFrameHeader();
                    }
                }
            }
        }

        /// <summary>
        /// フレームヘッダー
        /// MP3のFrameHeaderを取得しサンプル周波数などを取得する予定
        /// 未完 FrameHeaderの位置がよくわからん
        /// </summary>
        private void getFrameHeader()
        {
            int headerSize = 32;
            byte[] frameHeader = new byte[headerSize];
            int readBytes = mFileStream.Read(frameHeader, 0, frameHeader.Length);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(frameHeader, 0, 4);
            //int mFrameHeader = BitConverter.ToInt32(frameHeader, 4);
            ylib.binaryDump(frameHeader, 0, headerSize, "FreameHeader");

        }

        /// <summary>
        /// ID3V1のタグ取得
        /// </summary>
        private void getId3V1Tag()
        {
            Encoding encording = Encoding.Default;
            char[] trimChars = { '\0' };
            mVer = "ID3V1";
            mID3Tags.Clear();
            mTagData = new byte[125];
            int readBytes = mFileStream.Read(mTagData, 0, mTagData.Length);
            mTagSize = readBytes + 3;
            if (readBytes == mTagData.Length) {
                mID3Tags.Add("TITLE", encording.GetString(mTagData, 3 - 3, 30).TrimEnd(trimChars));
                mID3Tags.Add("ARTIST", encording.GetString(mTagData, 33 - 3, 30).TrimEnd(trimChars));
                mID3Tags.Add("ALBUM", encording.GetString(mTagData, 63 - 3, 30).TrimEnd(trimChars));
                mID3Tags.Add("YEAR", encording.GetString(mTagData, 93 - 3, 4).TrimEnd(trimChars));
                mID3Tags.Add("COMMENT", encording.GetString(mTagData, 97 - 3, 28).TrimEnd(trimChars));
                if (mGenreListV1.ContainsKey(mTagData[124]))
                    mID3Tags.Add("GENRE", mGenreListV1[mTagData[124]]);
                else
                    mID3Tags.Add("GENRE", "");
            }
        }


        /// <summary>
        /// ID3V2のタグ取得
        /// </summary>
        private void getId3V2Tag()
        {
            //  タグヘッダの取得(先頭のtag[3]("ID3")を覗いた7byteを取得)
            mTagData = new byte[7];
            int readBytes = mFileStream.Read(mTagData, 0, mTagData.Length);
            mTagSize = readBytes + 3;               //  タグサイズ(10byte)
            if (readBytes == mTagData.Length) {
                //  タグのバージョン
                int majorVersion = (int)mTagData[0];
                int revision = (int)mTagData[1];
                mVer = string.Format("ID3V2.{0}.{1}", majorVersion, revision);
                if (BitConverter.IsLittleEndian) {
                    Array.Reverse(mTagData, 3, 4);
                }
                mID3Tags.Clear();
                //  フレームヘッダの取得
                mFrameIdSize = majorVersion == 2 ? 3 : 4;       // V2.xのフレームIDサイズ
                mFrameSize = ylib.bit7ConvertLong(mTagData, 3); //  フレームサイズ(下位7bitのSynchsafe整数)
                //  フレームデータの読み込み
                mTagData = new byte[mFrameSize];
                readBytes = mFileStream.Read(mTagData, 0, mTagData.Length);
                mTagSize += readBytes;
                if (readBytes == mTagData.Length) {
                    //  ヘッダを飛ばしてタグを取得
                    for (int index = 0; index < mTagData.Length;) {
                        if (mTagData[index] == '\0')
                            break;

                        //  ID3v2のフレームヘッダ
                        string frameID = Encoding.ASCII.GetString(mTagData, index, mFrameIdSize);
                        index += mFrameIdSize;
                        //  フレームサイズ(フレームヘッダを除く) v2.4の時は下位7bit有効
                        if (BitConverter.IsLittleEndian) {
                            Array.Reverse(mTagData, index, mFrameIdSize);
                        }
                        int frameSize = (int)ylib.bitConvertLong(mTagData, index, 3);  //  フレームサイズ(3byte)
                        index += mFrameIdSize;

                        //  V2.3のフラグ(使わない)
                        if (majorVersion != 2) {
                            byte flag1 = mTagData[index++];
                            byte flag2 = mTagData[index++];
                        }
                        string content = "";
                        int nextIndex = index + frameSize;      //  次のフレームの位置保存
                        if (frameID[0] == 'T') {
                            //  テキストデータ
                            if (mTagData[index] == 0x01) {
                                content = getUnicode(mTagData, index + 1, nextIndex);
                            } else {
                                content = getShiftJis(mTagData, index + 1, nextIndex);
                            }
                        } else if (frameID.CompareTo("COMM") == 0
                            || frameID.CompareTo("COM") == 0) {         //  コメント
                            content = getComment(mTagData, index, nextIndex, majorVersion);
                        } else if (frameID.CompareTo("APIC") == 0       //  V2.3の画像データ
                            || frameID.CompareTo("PIC") == 0) {         //  V2.2の画像データ
                            ImageData imageData = getImageData(mTagData, index, nextIndex, majorVersion);
                            mImageData.Add(imageData);
                            content = imageData.ImageType + " : " + imageData.PictureType + " " + imageData.Description;
                        } else if (frameID.CompareTo("UFID") == 0       //  Unique file identifier
                            || frameID.CompareTo("UFI") == 0
                            || frameID.CompareTo("PRIV") == 0) {        //  Private frame
                            content = getShiftJis(mTagData, index, nextIndex);
                        } else {
                            //  バイナリデータ
                            //ylib.binaryDump(mTagData, index, 32, frameID + " " + frameSize + " " + content);
                        }
                        //  タグ情報の登録
                        content = ylib.trimControllCode(content).Trim();
                        if (!mID3Tags.ContainsKey(frameID)) {
                            mID3Tags.Add(frameID, content);
                        } else {
                            if (frameID.CompareTo("PIC") == 0 || frameID.CompareTo("APIC") == 0) {
                                mID3Tags[frameID] = mID3Tags[frameID] + " [" + mImageData.Count + "] " + content;
                            } else {
                                mID3Tags[frameID] = mID3Tags[frameID] + "," + content;
                            }
                        }
                        index = nextIndex;
                    }
                }
            }
        }

        /// <summary>
        /// COMMENTタグの取得
        /// </summary>
        /// <param name="tagData">タグデータ</param>
        /// <param name="index">開始位置</param>
        /// <param name="nextIndex">次のデータ位置</param>
        /// <param name="version">タグのバージョン</param>
        /// <returns></returns>
        private string getComment(byte[] tagData, int index, int nextIndex, int version)
        {
            int encord;
            string lang;
            string frameId = "";
            string content = "";
            int frameSize = nextIndex - index;
            Encoding encoding = Encoding.GetEncoding("shift-jis");   //  SHIF-JIS
            if (version == 2) {
                frameId = "COM";
                encord = tagData[index++];
                lang = encoding.GetString(tagData, index, 3);
                index += 3;
                content = getShiftJis(tagData, index, nextIndex);
            } else {
                frameId = "COMM";
                encord = tagData[index++];
                lang = encoding.GetString(tagData, index, 3);
                index += 3;
                if (encord == 0x01) {
                    content = getUnicode(tagData, index, nextIndex);
                    if ((index + 2 + content.Length * 2) < nextIndex)
                        content += getUnicode(tagData, index + 2 + content.Length * 2, nextIndex);
                } else {
                    content = getShiftJis(tagData, index, nextIndex);
                    if ((index + 1 + content.Length) < nextIndex)
                        content += getShiftJis(tagData, index + 1 + content.Length, nextIndex);
                }
            }
            //ylib.binaryDump(tagData, index, frameSize, frameId + " " + frameSize.ToString() + " " + content);
            return content;
        }

        /// <summary>
        /// Unicodeの文字列の取得
        /// </summary>
        /// <param name="tagData">タグデータ</param>
        /// <param name="index">開始位置</param>
        /// <param name="nextIndex">次のデータ位置</param>
        /// <returns></returns>
        private string getUnicode(byte[] tagData, int index, int nextIndex)
        {
            Encoding encoding;
            if (tagData[index] == 0xfe && tagData[index + 1] == 0xff) {
                encoding = Encoding.BigEndianUnicode;   //  UTF-16BE
            } else {
                encoding = Encoding.Unicode;            //  UTF-16LE
            }
            int i = 2;
            while ((tagData[index + i] != 0x00 || tagData[index + i + 1] != 0x00) && (index + i < nextIndex))
                i += 2;
            string content = encoding.GetString(tagData, index, i);
            return content;
        }

        /// <summary>
        /// ShiftJisコードの文字列を取得
        /// </summary>
        /// <param name="tagData">タグデータ</param>
        /// <param name="index">開始位置</param>
        /// <param name="nextIndex">次のデータ位置</param>
        /// <returns></returns>
        private string getShiftJis(byte[] tagData, int index, int nextIndex)
        {
            Encoding encoding = Encoding.GetEncoding("shift-jis");   //  SHIF-JIS
            int i = 0;
            while (tagData[index + i] != 0x00 && (index + i < nextIndex))
                i++;
            string content = encoding.GetString(tagData, index, i);
            return content;
        }

        /// <summary>
        /// タグの画像データの取得(PIC/APIC)
        /// </summary>
        /// <param name="tagData">タグデータ</param>
        /// <param name="index">画像データの開始位置</param>
        /// <param name="nextIndex">次のデータの位置</param>
        /// <param name="version">タグのバージョン</param>
        /// <returns></returns>
        private ImageData getImageData(byte[] tagData, int index, int nextIndex, int version)
        {
            Encoding encoding = Encoding.GetEncoding("shift-jis");   //  SHIF-JIS
            ImageData imageData = new ImageData();
            //ylib.binaryDump(tagData, index, 16, "PIC");
            int i = 1;
            if (version == 2) {
                while (tagData[index + i] != 0x00)
                    i++;
                imageData.FileExt = encoding.GetString(tagData, index + 1, i - 1);
                imageData.ImageType = imageData.FileExt;
                index += i;
            } else {
                //  MIME Type
                while (mTagData[index + i] != 0x00)
                    i++;
                imageData.ImageType = encoding.GetString(mTagData, index + 1, i - 1);
                imageData.FileExt = imageData.ImageType.Substring(imageData.ImageType.IndexOf("/") + 1);
                index += i + 1;
            }
            //  画像の種類
            //ylib.binaryDump(tagData, index, 16, "PIC type");
            byte picureType = tagData[index++];    //  データタイプの説明に変換
            imageData.PictureType = mPictureType[picureType];
            //  画像の説明の取り出し
            //ylib.binaryDump(mTagData, index, 32, "APIC Description: ");
            i = 0;
            while (tagData[index + i] != 0x00)
                i++;
            imageData.Description = encoding.GetString(tagData, index, i);
            //  画像データの取り出し
            index += i + 1;
            int dataSize = nextIndex - index;
            //ylib.binaryDump(mTagData, index, 128, "APIC ImageData: " + dataSize.ToString());
            imageData.PictureData = new byte[dataSize];
            for (int j = 0; j < dataSize; j++)
                imageData.PictureData[j] = tagData[index + j];

            return imageData;
        }

        /// <summary>
        /// V2.2.0のジャンル設定
        /// 分類が直接記載されている場合と数値で分類表を指定する場合がある
        /// 数値で指定する場合は数値が'('')'で囲まれている
        /// </summary>
        private void convertGenreV220()
        {
            string genre = "";
            string FrameID = "";
            if (mID3Tags.ContainsKey("TCO"))
                FrameID = "TCO";
            else if (mID3Tags.ContainsKey("TCON"))
                FrameID = "TCON";
            else
                return;

            genre = mID3Tags[FrameID];
            string buf = "";
            for (int i = 0; i < genre.Length; i++) {
                if (genre[i] == '(') {
                    //  分類表から取得
                    int n = (int)ylib.string2double(genre.Substring(i + 1));
                    if (mGenreListV220.ContainsKey(n)) {
                        buf += (0 < buf.Length ? " " : "") + mGenreListV220[n];
                    }
                }
            }
            if (0 < buf.Length)
                mID3Tags[FrameID] = buf;
        }


        /// <summary>
        /// ID3のバージョンの取得
        /// </summary>
        /// <returns></returns>
        public string getVersion()
        {
            return mVer;
        }

        /// <summary>
        /// タグサイズの取得
        /// </summary>
        /// <returns></returns>
        public long getTagSize()
        {
            if (mVer.CompareTo("FLAC") == 0) {
                return mFlacFileTagReader.mTagSize;
            } else if (mVer.CompareTo("ASF") == 0) {
                return mAsfFileTagReader.mTagSize; ;
            } else 
                return mTagSize;
        }

        /// <summary>
        /// タグデータをキーで取得
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string getTagData(string key)
        {
            if (mVer.CompareTo("FLAC") == 0) {
                if (mFlacFileTagReader.mFlacTags.ContainsKey(key))
                    return mFlacFileTagReader.mFlacTags[key];
            } else if (mVer.CompareTo("ASF") == 0) {
                if (mAsfFileTagReader.mAsfTags.ContainsKey(key))
                    return mAsfFileTagReader.mAsfTags[key];
            } else {
                if (mID3Tags.ContainsKey(key))
                    return mID3Tags[key];
            }
            return "";
        }

        /// <summary>
        /// タグデータを変換したキーで取得
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string getTagDataConvetKey(string key)
        {
            string[] keys;
            //  タグ変換テーブルでタグのキーワードを変換する
            if (mTagConverte.ContainsKey(key)) {
                keys = mTagConverte[key];
                int n = Array.IndexOf(mVerTitle, mVer);
                if (0 <= n)
                    key = keys[n];
            }
            return getTagData(key);
        }

        /// <summary>
        /// 演奏時間(ms)
        /// </summary>
        /// <returns></returns>
        public long getPlayLength()
        {
            if (mVer.CompareTo("ID3V1") == 0) {
                return 0;
            } else if (mVer.CompareTo("ID3V2.2.0") == 0) {
                if (mID3Tags.ContainsKey("TLE"))
                    return int.Parse(mID3Tags["TLE"]);
            } else if (mVer.CompareTo("ID3V2.3.0") == 0) {
                if (mID3Tags.ContainsKey("TLEN"))
                    return int.Parse(mID3Tags["TLEN"]);
            } else if (mVer.CompareTo("ID3V2.4.0") == 0) {

            } else if (mVer.CompareTo("FLAC") == 0) {

            } else if (mVer.CompareTo("ASF") == 0) {
                return mAsfFileTagReader.mPlayLength;
            }
            return 0;
        }

        /// <summary>
        /// サンプリング周波数(Hz)
        /// </summary>
        /// <returns></returns>
        public int getSampleRate()
        {
            if (mVer.CompareTo("ASF") == 0) {
                return mAsfFileTagReader.mSampleRate;
            } else if (mVer.CompareTo("FLAC") == 0) {
                return 0;
            } else {
                return 0;
            }
        }

        /// <summary>
        /// 量子化ビット(8/16/24/32 bit)
        /// </summary>
        /// <returns></returns>
        public int getBitsPerSample()
        {
            if (mVer.CompareTo("ASF") == 0) {
                return mAsfFileTagReader.mBitPerSample;
            } else if (mVer.CompareTo("FLAC") == 0) {
                return 0;
            } else {
                return 0;
            }
        }

        /// <summary>
        /// サンプルビットレート(bps)
        /// </summary>
        /// <returns></returns>
        public int getSampleBitsPerRate()
        {
            if (mVer.CompareTo("ASF") == 0) {
                return mAsfFileTagReader.mSampleBitRate;
            } else if (mVer.CompareTo("FLAC") == 0) {
                return 0;
            } else {
                return 0;
            }
        }

        /// <summary>
        /// チャンネル数
        /// </summary>
        /// <returns></returns>
        public int getChannels()
        {
            if (mVer.CompareTo("ASF") == 0) {
                return mAsfFileTagReader.mChanneles;
            } else if (mVer.CompareTo("FLAC") == 0) {
                return 0;
            } else {
                return 0;
            }
        }

        /// <summary>
        /// ASFファイルのデータの長さ
        /// </summary>
        /// <returns></returns>
        public long getPlaylength()
        {
            if (mVer.CompareTo("ASF") == 0) {
                return mAsfFileTagReader.mDataLength;
            } else if (mVer.CompareTo("FLAC") == 0) {
                return 0;
            } else {
                return 0;
            }
        }


        /// <summary>
        /// タグリストの取得
        /// </summary>
        /// <returns></returns>
        public List<string> getTagList()
        {
            List<string> tagList = new List<string>();
            tagList.Add("[" + mVer + "]");
            if (mVer.CompareTo("ID3V1") == 0) {
                foreach (KeyValuePair<string, string> item in mID3Tags) {
                    tagList.Add(item.Key);
                    tagList.Add("  " + item.Value);
                }
            } else if (mVer.CompareTo("ID3V2.2.0") == 0) {
                foreach (KeyValuePair<string, string> item in mID3Tags) {
                    if (mTagV220Name.ContainsKey(item.Key))
                        tagList.Add(mTagV220Name[item.Key]);
                    else
                        tagList.Add(item.Key);
                    tagList.Add("  " + item.Value);
                }
            } else if (mVer.CompareTo("ID3V2.3.0") == 0) {
                foreach (KeyValuePair<string, string> item in mID3Tags) {
                    if (mTagV230Name.ContainsKey(item.Key))
                        tagList.Add(mTagV230Name[item.Key]);
                    else
                        tagList.Add(item.Key);
                    tagList.Add("  " + item.Value);
                }
            } else if (mVer.CompareTo("ID3V2.4.0") == 0) {

            } else if (mVer.CompareTo("FLAC") == 0) {
                return mFlacFileTagReader.mFlacTagList;
            } else if (mVer.CompareTo("ASF") == 0) {
                return mAsfFileTagReader.mAsfTagList;
            }
            return tagList;
        }

        /// <summary>
        /// 登録されているイメージデータの数
        /// </summary>
        /// <returns></returns>
        public int getImageDataCount()
        {
            if (mVer.CompareTo("ASF") == 0) {
                return mAsfFileTagReader.mImageData.Count;
            } else if (mVer.CompareTo("FLAC") == 0) {
                return mFlacFileTagReader.mImageData.Count;
            } else {
                return mImageData.Count;
            }
        }

        /// <summary>
        /// イメージデータのファイル拡張子を取得
        /// </summary>
        /// <returns></returns>
        public string getImageExt(int n)
        {
            if (mVer.CompareTo("ASF") == 0) {
                if (0 < mAsfFileTagReader.mImageData.Count)
                    return mAsfFileTagReader.mImageData[n].FileExt;
            } else if (mVer.CompareTo("FLAC") == 0) {
            } else {
                if (0 < mImageData.Count)
                    return mImageData[n].FileExt;
            }
            return "";
        }

        /// <summary>
        /// イメージデータのサイズの取得、ない場合は0を返す
        /// </summary>
        /// <returns></returns>
        public int getImageDataSize(int n)
        {
            if (mVer.CompareTo("ASF") == 0) {
                if (0 < mAsfFileTagReader.mImageData.Count)
                    return mAsfFileTagReader.mImageData[n].PictureData.Length;
            } else if (mVer.CompareTo("FLAC") == 0) {
                if (0 < mFlacFileTagReader.mImageData.Count)
                    return mFlacFileTagReader.mImageData[n].PictureData.Length;
            } else {
                if (0 < mImageData.Count)
                    return mImageData[n].PictureData.Length;
            }
            return 0;
        }

        /// <summary>
        /// イメージデータの取得
        /// </summary>
        /// <returns></returns>
        public byte[] getImageData(int n)
        {
            if (mVer.CompareTo("ASF") == 0) {
                if (0 < mAsfFileTagReader.mImageData.Count)
                    return mAsfFileTagReader.mImageData[n].PictureData;
            } else if (mVer.CompareTo("FLAC") == 0) {
                if (0 < mFlacFileTagReader.mImageData.Count)
                    return mFlacFileTagReader.mImageData[n].PictureData;
            } else {
                if (0 < mImageData.Count)
                    return mImageData[n].PictureData;
            }
            return null;
        }
    }

    /// <summary>
    /// イメージデータクラス
    /// </summary>
    public class ImageData
    {
        public string ImageType { set; get; }   //  ImageDataの種類(MimeType)
        public string FileExt { set; get; }     //  Imadataのファイル拡張子
        public string PictureType { set; get; } //  PictureType (Cover...)
        public string Description { set; get; } //  データの説明
        public byte[] PictureData { set; get; } //  ImageData
    }
}
