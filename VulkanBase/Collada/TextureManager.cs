using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace VulkanBase.Collada
{
    public class TextureManager
    {
        private delegate void BindDelegate();
        private BindDelegate _bindCommand;

        private int[] _textureIdArray;

        private ObservableCollection<Texture> _textures;
        public ObservableCollection<Texture> Textures
        {
            get
            {
                return _textures;
            }
            set
            {
                _textures = value;
                _textures.CollectionChanged += OnTextureListChanged;
                OnTextureListChanged(_textures, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

        private void OnTextureListChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            _textureIdArray = _textures.Select(texture => texture.Id).ToArray();
            _bindCommand = new BindDelegate(FirstTimeBind);
        }


        public TextureManager()
        {
            _textures = new ObservableCollection<Texture>();
            _textures.CollectionChanged += OnTextureListChanged;
            _bindCommand = new BindDelegate(FirstTimeBind);
        }


        public void BindTexture()
        {
            _bindCommand.Invoke();
        }

        private void FirstTimeBind()
        {
            for (int i = 0; i < Textures.Count; i++)
            {
                Textures[i].Bind();
            }

            // _bindCommand = new BindDelegate(FastBind);
        }

        private void FastBind()
        {
            //GL.BindTextures(0, _textureIdArray.Length, _textureIdArray);
        }

        public void AddTexture(Texture texture)
        {
            Textures.Add(texture);
        }

        public void RemoveLastTexture()
        {
            Textures.RemoveAt(Textures.Count - 1);
        }
    }
}