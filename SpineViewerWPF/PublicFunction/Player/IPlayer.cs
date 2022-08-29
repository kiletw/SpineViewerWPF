using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public interface IPlayer
{
    void Initialize();

    void LoadContent(ContentManager contentManager);


    void Update(GameTime gameTime);

    void Draw();

    void ChangeSet();

    void SizeChange();

    void Dispose();
}

