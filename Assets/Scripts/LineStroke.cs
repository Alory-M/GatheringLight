using System;
using System.Collections.Generic;

namespace PixelMoments
{
    // Traverse every grid cell intersected by each actual pointer segment, in any direction.
    public sealed class LineStroke
    {
        private readonly HashSet<int> visited=new HashSet<int>();
        private double lastRow,lastCol;
        public Mark Mark { get; }
        public LineStroke(int row,int col,Mark mark):this(row+.5,col+.5,mark) {}
        public LineStroke(double row,double col,Mark mark) {lastRow=row;lastCol=col;Mark=mark;}
        public void Visit(int row,int col,Action<int,int,Mark> apply) => Visit(row+.5,col+.5,apply);
        public void Visit(double row,double col,Action<int,int,Mark> apply)
        {
            double x=lastCol,y=lastRow,dx=col-x,dy=row-y;lastRow=row;lastCol=col;
            double begin=0,end=1,max=Puzzle.Size-1e-7;
            // Clip segments, rather than clamping the finger, to avoid painting imaginary edge trails.
            if(!Clip(-dx,x,ref begin,ref end) || !Clip(dx,max-x,ref begin,ref end) ||
               !Clip(-dy,y,ref begin,ref end) || !Clip(dy,max-y,ref begin,ref end))return;
            double endX=x+dx*end,endY=y+dy*end;
            x+=dx*begin;y+=dy*begin;dx=endX-x;dy=endY-y;
            int c=Index(x),r=Index(y),targetC=Index(endX),targetR=Index(endY);
            int stepC=Math.Sign(dx),stepR=Math.Sign(dy);
            double nextC=stepC==0?double.PositiveInfinity:((stepC>0?c+1:c)-x)/dx;
            double nextR=stepR==0?double.PositiveInfinity:((stepR>0?r+1:r)-y)/dy;
            double deltaC=stepC==0?double.PositiveInfinity:1/Math.Abs(dx);
            double deltaR=stepR==0?double.PositiveInfinity:1/Math.Abs(dy);
            for(int guard=0;guard<Puzzle.Size*2+1;guard++) {
                if(visited.Add(r*Puzzle.Size+c))apply(r,c,Mark);
                if(r==targetR && c==targetC)break;
                if(Math.Abs(nextC-nextR)<1e-10) {c+=stepC;r+=stepR;nextC+=deltaC;nextR+=deltaR;}
                else if(nextC<nextR) {c+=stepC;nextC+=deltaC;}
                else {r+=stepR;nextR+=deltaR;}
                if(r<0 || r>=Puzzle.Size || c<0 || c>=Puzzle.Size)break;
            }
        }
        private static int Index(double coordinate) => Math.Max(0,Math.Min(Puzzle.Size-1,(int)Math.Floor(coordinate)));
        private static bool Clip(double p,double q,ref double begin,ref double end)
        {
            if(Math.Abs(p)<1e-12)return q>=0;
            double t=q/p;
            if(p<0) {if(t>end)return false;begin=Math.Max(begin,t);}
            else {if(t<begin)return false;end=Math.Min(end,t);}
            return true;
        }
    }
}
