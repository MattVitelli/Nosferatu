using System;
using System.Collections.Generic;
using Gaia.Animation;
using Gaia.Core;
using Microsoft.Xna.Framework;

namespace Gaia.SceneGraph.GameEntities
{
    public class AnimatedModel : Entity
    {
        public ViewModel Model;

        bool isVisible = true;

        public AnimatedModel(string name)
        {
            Model = new ViewModel(name);
        }

        public void SetVisible(bool visible)
        {
            isVisible = visible;
        }

        public override void OnAdd(Scene scene)
        {
            base.OnAdd(scene);
            Model.SetTransform(this.Transformation);
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            Model.OnUpdate();
        }

        public override void OnRender(Gaia.Rendering.RenderViews.RenderView view)
        {
            base.OnRender(view);
            if(isVisible)
                Model.OnRender(view, true);
        }
    }
}
