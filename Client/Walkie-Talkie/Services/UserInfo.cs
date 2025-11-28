using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Walkie_Talkie.Services
{
    public enum UserStatus
    {
        Inactive = 0,
        Active = 1,
        Speaking = 2,
        Mute = 3,
        Private = 4,
    }
    public enum Authority
    {
        Normal = 0,
        Admin = 1,
    }

    public class UserInfo : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string _userName;
        public string UserName
        {
            get => _userName;
            set
            {
                if (_userName != value)
                {
                    _userName = value;
                    OnPropertyChanged(nameof(UserName));
                }
            }
        }

        private UserStatus _state;
        public UserStatus State
        {
            get => _state;
            set
            {
                if (_state != value)
                {
                    _state = value;
                    OnPropertyChanged(nameof(State));
                    OnPropertyChanged(nameof(StateName));
                    SetBGColor();
                }
            }
        }

        public bool IsSpeaking { get; set; }

        public bool IsMuted { get; set; }
        public string StateName => State.ToString();

        private string _avatarUrl;
        public string AvatarUrl
        {
            get => _avatarUrl;
            set
            {
                if (_avatarUrl != value)
                {
                    _avatarUrl = value;
                    OnPropertyChanged(nameof(AvatarUrl));
                }
            }
        }

        private Color _backGroundColor;
        public Color BackGroundColor
        {
            get => _backGroundColor;
            private set
            {
                if (_backGroundColor != value)
                {
                    _backGroundColor = value;
                    OnPropertyChanged(nameof(BackGroundColor));
                }
            }
        }
        private Authority _authority;
        public Authority Authority
        {
            get => _authority;
            private set
            {
                if (_authority != value)
                {
                    _authority = value;
                    OnPropertyChanged(nameof(Authority));
                }
            }
        }

        public UserInfo()
        {
            var random = new Random();
            string[] icons = new string[]
            {
                "avatar_m.png",
                "avatar_f.png",
                "avatar_1.png",
                "avatar_2.png"
            };
            AvatarUrl = icons[random.Next(icons.Length)];
            State = UserStatus.Inactive;
            Authority = Authority.Normal;
            SetBGColor();
        }
        public void SetAdmin()
        {
            Authority = Authority.Admin;
        }
        public void SetBGColor()
        {
            switch (State)
            {
                case UserStatus.Inactive:
                    BackGroundColor = Color.FromArgb("#616060");
                    break;
                case UserStatus.Active:
                    BackGroundColor = Color.FromArgb("#F0F0F0");
                    break;
                case UserStatus.Speaking:
                    BackGroundColor = Color.FromArgb("#4FC14B");
                    break;
                case UserStatus.Mute:
                    BackGroundColor = Color.FromArgb("#FF5858");
                    break;
                case UserStatus.Private:
                    BackGroundColor = Color.FromArgb("#98C54C");
                    break;
                default:
                    BackGroundColor = Color.FromArgb("#F0F0F0");
                    break;
            }
        }

        protected void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    }
}
