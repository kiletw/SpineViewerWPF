using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class GlobalValue : INotifyPropertyChanged
{
    private string _SelectFile = "";
    private List<string> _AnimeList;
    private List<string> _SkinList;
    private float _Scale;
    private int _Speed = 24;
    private float _PosX = 0;
    private float _PosY = 0;
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

    public string SelectFile
    {
        get
        {
            return _SelectFile;
        }
        set
        {
            if (_SelectFile != value)
            {
                _SelectFile = value;
                OnPropertyChanged("SelectFile");
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
                _Scale = (float)Math.Round(value,2);
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
            if(int.TryParse(value.ToString(),out _Speed))
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
            return _FrameHeight-20;
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

    public event PropertyChangedEventHandler PropertyChanged;
    public void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }


}

