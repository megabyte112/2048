using Microsoft.Xna.Framework;

namespace _2048
{
/*  the tile class:
    each tile holds its value, as well as information
    about itself, such as if it's just been merged,
    as well as animation information.                   */
    public class Tile
    {
        public int Value {get; set;}
        public int OldValue {get; set;}
        public bool HasMerged {get; set;}
        private int FadeIn {get; set;}
        public int Fade {get {return FadeIn;}}
        public bool doFadeOut {get; set;}
        public int FadeOutOld {get; set;}
        public float Opacity {get; set;}
        public double AnimationTimer {get; set;}
        public string AnimationDirection {get; set;} // "up", "down", "left", "right", "none"
        public Tile(int value)
        {
            Value = value;
            HasMerged = false;
            AnimationTimer = 0;
            AnimationDirection = "none";
            FadeIn = 20;
            doFadeOut = false;
            Opacity = 1f;
        }

        // offset is used by the animation to find where to draw the sprite
        public Vector2 Offset()
        {
            switch (AnimationDirection)
            {
                case "up":
                    return new Vector2(0, (float)AnimationTimer);
                case "down":
                    return new Vector2(0, -(float)AnimationTimer);
                case "left":
                    return new Vector2((float)AnimationTimer, 0);
                case "right":
                    return new Vector2(-(float)AnimationTimer, 0);
                default:
                    return Vector2.Zero;
            }
        }
        
        // store offset of old tile for smooth animation
        public double OldTimer {get; set;}

        // used to render the ghost tile during animation
        public Vector2 OldOffset()
        {
            switch (AnimationDirection)
            {
                case "up":
                    return new Vector2(0, (float)OldTimer);
                case "down":
                    return new Vector2(0, -(float)OldTimer);
                case "left":
                    return new Vector2((float)OldTimer, 0);
                case "right":
                    return new Vector2(-(float)OldTimer, 0);
                default:
                    return Vector2.Zero;
            }
        }
        
        // returns the opacity that the sprite should be rendered at,
        // used for fade animation
        public float GetAlpha()
        {
            if (FadeIn > 10) return 0f;
            else if (FadeIn > 1) return (10f-(float)FadeIn)/10f;
            else return Opacity;
        }
        public float GetOldAlpha()
        {
            if (FadeOutOld > 10) return 1f;
            return (float)FadeOutOld/10f;
        }

        // updates animation timers
        // slowAnimate is used in debug mode
        public void UpdateTile(bool slowAnimate)
        {
            if (AnimationTimer>384d) AnimationTimer = 384d;
            if (OldTimer>384d) OldTimer = 384d;
            if (AnimationTimer > 2&&!slowAnimate) AnimationTimer*=0.75d;
            else if (AnimationTimer > 2&&slowAnimate) AnimationTimer*=0.96875d;
            else AnimationTimer = 0;
            if (OldTimer > 2&&!slowAnimate) OldTimer*=0.75d;
            else if (OldTimer > 2&&slowAnimate) OldTimer*=0.96875d;
            else OldTimer = 0;
            if (FadeIn > 1) FadeIn--;
            if (FadeOutOld > 0) FadeOutOld--;
            if (doFadeOut&&Opacity>0) Opacity-=0.125f;
            else if (doFadeOut&&Opacity==0)
            {
                Value = 0;
                Opacity = 1f;
                doFadeOut = false;
                _2048.shouldresetgrid = true;
            }
        }
    }
}
