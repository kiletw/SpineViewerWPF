using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class GlobalValue : INotifyPropertyChanged
{
    private string _SelectAtlasFile = "";
    private string _SelectSpineFile = "";
    private List<string> _AnimeList;
    private List<string> _SkinList;
    private float _Scale;
    private int _Speed = 30;
    private float _PosX = 0;
    private float _PosY = 0;
    private float _PosBGX = 0;
    private float _PosBGY = 0;
    private bool _UseBG = false;
    private string _SelectBG = "";
    private bool _ControlBG = false;
    private bool _Alpha;
    private bool _IsLoop;
    private string _SelectAnimeName = "";
    private string _SelectSkin = "";
    private float _TimeScale = 1;
    private string _SpineVersion = "";
    private double _FrameWidth;
    private double _FrameHeight;
    private bool _PreMultiplyAlpha;
    private bool _SetSkin = false;
    private bool _SetAnime = false;
    private string _FileHash = "";
    private string _GifQuality = "Default";
    private string _LoadingProcess = "0%";
    private float _Lock = 0f;
    private bool _IsRecoding = false;
    private bool _FilpX = false;
    private bool _FilpY = false;
    private float _RedcodePanelWidth = 280f;
    private float _Rotation = 0;

    private List<Texture2D> _GifList;


    public string SelectAtlasFile
    {
        get
        {
            return _SelectAtlasFile;
        }
        set
        {
            if (_SelectAtlasFile != value)
            {
                _SelectAtlasFile = value;
                OnPropertyChanged("SelectAtlasFile");
            }
        }
    }

    public string SelectSpineFile
    {
        get
        {
            return _SelectSpineFile;
        }
        set
        {
            if (_SelectSpineFile != value)
            {
                _SelectSpineFile = value;
                OnPropertyChanged("SelectSpineFile");
            }
        }
    }

    public List<string> AnimeList
    {
        get
        {
            return _AnimeList;
        }
        set
        {
            if (_AnimeList != value)
            {
                _AnimeList = value;
                OnPropertyChanged("AnimeList");
            }
        }
    }
    public List<string> SkinList
    {
        get
        {
            return _SkinList;
        }
        set
        {
            if (_SkinList != value)
            {
                _SkinList = value;
                OnPropertyChanged("SkinList");
            }
        }
    }
    public float Scale
    {
        get
        {
            return _Scale;
        }
        set
        {
            if (_Scale != value)
            {
                _Scale = (float)Math.Round(value, 2);
                OnPropertyChanged("Scale");
            }
        }
    }
    public int Speed
    {
        get
        {
            return _Speed;
        }
        set
        {
            if (int.TryParse(value.ToString(), out _Speed))
            {
                if (_Speed != value)
                {
                    _Speed = value;
                    OnPropertyChanged("Speed");
                }
            }
        }
    }
    public float PosX
    {
        get
        {
            return _PosX;
        }
        set
        {
            if (_PosX != value)
            {
                _PosX = (float)Math.Round(value, 2);
                OnPropertyChanged("PosX");
            }
        }
    }
    public float PosY
    {
        get
        {
            return _PosY;
        }
        set
        {
            if (_PosY != value)
            {
                _PosY = (float)Math.Round(value, 2);
                OnPropertyChanged("PosY");
            }
        }
    }

    public float PosBGX
    {
        get
        {
            return _PosBGX;
        }
        set
        {
            if (_PosBGX != value)
            {
                _PosBGX = (float)Math.Round(value, 2);
                OnPropertyChanged("PosBGX");
            }
        }
    }
    public float PosBGY
    {
        get
        {
            return _PosBGY;
        }
        set
        {
            if (_PosBGY != value)
            {
                _PosBGY = (float)Math.Round(value, 2);
                OnPropertyChanged("PosBGY");
            }
        }
    }
    public bool Alpha
    {
        get
        {
            return _Alpha;
        }
        set
        {
            if (_Alpha != value)
            {
                _Alpha = value;
                OnPropertyChanged("Alpha");
            }
        }
    }
    public bool UseBG
    {
        get
        {
            return _UseBG;
        }
        set
        {
            if (_UseBG != value)
            {
                _UseBG = value;
                OnPropertyChanged("UseBG");
            }
        }
    }
    public bool ControlBG
    {
        get
        {
            return _ControlBG;
        }
        set
        {
            if (_ControlBG != value)
            {
                _ControlBG = value;
                OnPropertyChanged("ControlBG");
            }
        }
    }
    public bool IsLoop
    {
        get
        {
            return _IsLoop;
        }
        set
        {
            if (_IsLoop != value)
            {
                _IsLoop = value;
                OnPropertyChanged("IsLoop");
            }
        }
    }
    public string SelectAnimeName
    {
        get
        {
            return _SelectAnimeName;
        }
        set
        {
            if (_SelectAnimeName != value)
            {
                _SelectAnimeName = value;
                OnPropertyChanged("SelectAnimeName");
            }
        }
    }
    public string SelectSkin
    {
        get
        {
            return _SelectSkin;
        }
        set
        {
            if (_SelectSkin != value)
            {
                _SelectSkin = value;
                OnPropertyChanged("SelectSkin");
            }
        }
    }
    public string SelectBG
    {
        get
        {
            return _SelectBG;
        }
        set
        {
            if (_SelectBG != value)
            {
                _SelectBG = value;
                OnPropertyChanged("SelectBG");
            }
        }
    }

    public float TimeScale
    {
        get
        {
            return _TimeScale;
        }
        set
        {
            if (_TimeScale != value)
            {
                _TimeScale = value;
                OnPropertyChanged("TimeScale");
            }
        }
    }
    public string SpineVersion
    {
        get
        {
            return _SpineVersion;
        }
        set
        {
            if (_SpineVersion != value)
            {
                _SpineVersion = value;
                OnPropertyChanged("SpineVersion");
            }
        }
    }
    public double FrameWidth
    {
        get
        {
            return _FrameWidth;
        }
        set
        {
            if (_FrameWidth != value)
            {
                _FrameWidth = value;
                OnPropertyChanged("FrameWidth");
            }
        }
    }
    public double FrameHeight
    {
        get
        {
            return _FrameHeight - 20;
        }
        set
        {
            if (_FrameHeight != value)
            {
                _FrameHeight = value;
                OnPropertyChanged("FrameHeight");
            }
        }
    }

    public bool PreMultiplyAlpha
    {
        get
        {
            return _PreMultiplyAlpha;
        }
        set
        {
            if (_PreMultiplyAlpha != value)
            {
                _PreMultiplyAlpha = value;
                OnPropertyChanged("PreMultiplyAlpha");
            }
        }
    }

    public bool SetSkin
    {
        get
        {
            return _SetSkin;
        }
        set
        {
            if (_SetSkin != value)
            {
                _SetSkin = value;
                OnPropertyChanged("SetSkin");
            }
        }
    }

    public bool SetAnime
    {
        get
        {
            return _SetAnime;
        }
        set
        {
            if (_SetAnime != value)
            {
                _SetAnime = value;
                OnPropertyChanged("SetAnime");
            }
        }
    }

    public string FileHash
    {
        get
        {
            return _FileHash;
        }
        set
        {
            if (_FileHash != value)
            {
                _FileHash = value;
                OnPropertyChanged("FileHash");
            }
        }
    }

    public string GifQuality
    {
        get
        {
            return _GifQuality;
        }
        set
        {
            if (_GifQuality != value)
            {
                _GifQuality = value;
                OnPropertyChanged("GifQuality");
            }
        }
    }
    public string LoadingProcess
    {
        get
        {
            return _LoadingProcess;
        }
        set
        {
            if (_LoadingProcess != value)
            {
                _LoadingProcess = value;
                OnPropertyChanged("LoadingProcess");
            }

        }
    }

    public float Lock
    {
        get
        {
            return _Lock;
        }
        set
        {
            if (float.TryParse(value.ToString(), out _Lock))
            {
                _Lock = (float)Math.Round(value, 2);
                OnPropertyChanged("Lock");
            }
        }
    }

    public List<Texture2D> GifList
    {

        get
        {
            if (_GifList == null)
                _GifList = new List<Texture2D>();


            return _GifList;
        }
        set
        {
            if (_GifList != value)
            {
                _GifList = value;
            }
           

        }
    }

    public bool IsRecoding
    {
        get
        {
            return _IsRecoding;
        }
        set
        {
            if (_IsRecoding != value)
            {
                _IsRecoding = value;
                OnPropertyChanged("IsRecoding");
            }
        }
    }

    public bool FilpX
    {
        get
        {
            return _FilpX;
        }
        set
        {
            if (_FilpX != value)
            {
                _FilpX = value;
                OnPropertyChanged("FilpX");
            }
        }
    }

    public bool FilpY
    {
        get
        {
            return _FilpY;
        }
        set
        {
            if (_FilpY != value)
            {
                _FilpY = value;
                OnPropertyChanged("FilpY");
            }
        }
    }

    public float RedcodePanelWidth
    {
        get
        {
            return _RedcodePanelWidth;
        }
        set
        {
            if (float.TryParse(value.ToString(), out _RedcodePanelWidth))
            {
                if (_RedcodePanelWidth != value)
                {
                    _RedcodePanelWidth = value;
                    OnPropertyChanged("RedcodePanelWidth");
                }
            }
        }
    }

    public float Rotation
    {
        get
        {
            return _Rotation;
        }
        set
        {
            if (float.TryParse(value.ToString(), out _Rotation))
            {
                if (_Rotation != value)
                {
                    _Rotation = value;
                    OnPropertyChanged("Rotation");
                }
            }
        }
    }



    public event PropertyChangedEventHandler PropertyChanged;
    public void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }


}

