/// <copyright>
///
/// Rewritten in C# by Yury Kiselev, 2010.
///
/// Copyright (C) 1996-1997 Id Software, Inc.
///
/// This program is free software; you can redistribute it and/or
/// modify it under the terms of the GNU General Public License
/// as published by the Free Software Foundation; either version 2
/// of the License, or (at your option) any later version.
/// 
/// This program is distributed in the hope that it will be useful,
/// but WITHOUT ANY WARRANTY; without even the implied warranty of
/// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  
/// 
/// See the GNU General Public License for more details.
/// 
/// You should have received a copy of the GNU General Public License
/// along with this program; if not, write to the Free Software
/// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
/// </copyright>

using System;
using System.Text;
using System.IO;

namespace SharpQuake
{
    partial class Client
    {
        /// <summary>
        /// CL_Record_f
        /// record <demoname> <map> [cd track]
        /// </summary>
        static void Record_f()
        {
            if (Cmd.Source != cmd_source_t.src_command)
            {
                return;
            }

            int c = Cmd.Argc;
            if (c != 2 && c != 3 && c != 4)
            {
                Con.Print("record <demoname> [<map> [cd track]]\n");
                return;
            }

            if (Cmd.Argv(1).Contains(".."))
            {
                Con.Print("Relative pathnames are not allowed.\n");
                return;
            }

            if (c == 2 && Cls.state == ClientActivityState.Connected)
            {
                Con.Print("Can not record - already connected to server\nClient demo recording must be started before connecting\n");
                return;
            }

            // write the forced cd track number, or -1
            int track;
            if (c == 4)
            {
                track = Common.atoi(Cmd.Argv(3));
                Con.Print("Forcing CD track to {0}\n", track);
            }
            else
            {
                track = -1;
            }

            string name = Path.Combine(Common.GameDir, Cmd.Argv(1));

            //
            // start the map up
            //
            if (c > 2)
            {
                Cmd.ExecuteString(String.Format("map {0}", Cmd.Argv(2)), cmd_source_t.src_command);
            }

            //
            // open the demo file
            //
            name = Path.ChangeExtension(name, ".dem");

            Con.Print("recording to {0}.\n", name);
            FileStream fs = Sys.FileOpenWrite(name, true);
            if (fs == null)
            {
                Con.Print("ERROR: couldn't open.\n");
                return;
            }
            BinaryWriter writer = new BinaryWriter(fs, Encoding.ASCII);
            Cls.demofile = new DisposableWrapper<BinaryWriter>(writer, true);
            Cls.forcetrack = track;
            byte[] tmp = Encoding.ASCII.GetBytes(Cls.forcetrack.ToString());
            writer.Write(tmp);
            writer.Write('\n');
            Cls.demorecording = true;
        }

        
        /// <summary>
        /// CL_Stop_f
        /// stop recording a demo
        /// </summary>
        static void Stop_f()
        {
            if (Cmd.Source != cmd_source_t.src_command)
            {
                return;
            }

            if (!Cls.demorecording)
            {
                Con.Print("Not recording a demo.\n");
                return;
            }

            // write a disconnect message to the demo file
            Net.Message.Clear();
            Net.Message.WriteByte(Protocol.svc_disconnect);
            WriteDemoMessage();

            // finish up
            if (Cls.demofile != null)
            {
                Cls.demofile.Dispose();
                Cls.demofile = null;
            }
            Cls.demorecording = false;
            Con.Print("Completed demo\n");
        }

        
        // CL_PlayDemo_f
        //
        // play [demoname]
        static void PlayDemo_f()
        {
            if (Cmd.Source != cmd_source_t.src_command)
            {
                return;
            }

            if (Cmd.Argc != 2)
            {
                Con.Print("play <demoname> : plays a demo\n");
                return;
            }

            //
            // disconnect from server
            //
            Client.Disconnect();

            //
            // open the demo file
            //
            string name = Path.ChangeExtension(Cmd.Argv(1), ".dem");

            Con.Print("Playing demo from {0}.\n", name);
            if (Cls.demofile != null)
            {
                Cls.demofile.Dispose();
            }
            DisposableWrapper<BinaryReader> reader;
            Common.FOpenFile(name, out reader);
            Cls.demofile = reader;
            if (Cls.demofile == null)
            {
                Con.Print("ERROR: couldn't open.\n");
                Cls.demonum = -1;		// stop demo loop
                return;
            }

            Cls.demoplayback = true;
            Cls.state = ClientActivityState.Connected;
            Cls.forcetrack = 0;

            BinaryReader s = reader.Object;
            int c;
            bool neg = false;
            while (true)
            {
                c = s.ReadByte();
                if (c == '\n')
                {
                    break;
                }

                if (c == '-')
                {
                    neg = true;
                }
                else
                {
                    Cls.forcetrack = Cls.forcetrack * 10 + (c - '0');
                }
            }

            if (neg)
            {
                Cls.forcetrack = -Cls.forcetrack;
            }
            // ZOID, fscanf is evil
            //	fscanf (cls.demofile, "%i\n", &cls.forcetrack);
        }

        /// <summary>
        /// CL_TimeDemo_f
        /// timedemo [demoname]
        /// </summary>
        static void TimeDemo_f()
        {
	        if (Cmd.Source != cmd_source_t.src_command)
            {
                return;
            }

            if (Cmd.Argc != 2)
	        {
		        Con.Print("timedemo <demoname> : gets demo speeds\n");
		        return;
	        }

            PlayDemo_f();
	
            // cls.td_starttime will be grabbed at the second frame of the demo, so
            // all the loading time doesn't get counted
            _Static.timedemo = true;
            _Static.td_startframe = Host.FrameCount;
            _Static.td_lastframe = -1;		// get a new message this frame
        }

        
        /// <summary>
        /// CL_GetMessage 
        /// Handles recording and playback of demos, on top of NET_ code
        /// </summary>
        /// <returns></returns>
        static int GetMessage()
        {
            if (Cls.demoplayback)
            {
                // decide if it is time to grab the next message		
                if (Cls.signon == SIGNONS)	// allways grab until fully connected
                {
                    if (Cls.timedemo)
                    {
                        if (Host.FrameCount == Cls.td_lastframe)
                        {
                            return 0;      // allready read this frame's message
                        }

                        Cls.td_lastframe = Host.FrameCount;
                        // if this is the second frame, grab the real td_starttime
                        // so the bogus time on the first frame doesn't count
                        if (Host.FrameCount == Cls.td_startframe + 1)
                        {
                            Cls.td_starttime = (float)Host.RealTime;
                        }
                    }
                    else if (Cl.time <= Cl.mtime[0])
                    {
                        return 0;	// don't need another message yet
                    }
                }

                // get the next message
                BinaryReader reader = ((DisposableWrapper<BinaryReader>)Cls.demofile).Object;
                int size = Common.LittleLong(reader.ReadInt32());
                if (size > QDef.MAX_MSGLEN)
                {
                    Sys.Error("Demo message > MAX_MSGLEN");
                }

                Cl.mviewangles[1] = Cl.mviewangles[0];
                Cl.mviewangles[0].X = Common.LittleFloat(reader.ReadSingle());
                Cl.mviewangles[0].Y = Common.LittleFloat(reader.ReadSingle());
                Cl.mviewangles[0].Z = Common.LittleFloat(reader.ReadSingle());

                Net.Message.FillFrom(reader.BaseStream, size);
                if (Net.Message.Length < size)
                {
                    StopPlayback();
                    return 0;
                }
                return 1;
            }

            int r;
            while (true)
            {
                r = Net.GetMessage(Cls.netcon);

                if (r != 1 && r != 2)
                {
                    return r;
                }

                // discard nop keepalive message
                if (Net.Message.Length == 1 && Net.Message.Data[0] == Protocol.svc_nop)
                {
                    Con.Print("<-- server to client keepalive\n");
                }
                else
                {
                    break;
                }
            }

            if (Cls.demorecording)
            {
                WriteDemoMessage();
            }

            return r;
        }

        /// <summary>
        /// CL_StopPlayback
        /// 
        /// Called when a demo file runs out, or the user starts a game
        /// </summary>
        public static void StopPlayback()
        {
            if (!Cls.demoplayback)
            {
                return;
            }

            if (Cls.demofile != null)
            {
                Cls.demofile.Dispose();
                Cls.demofile = null;
            }
            Cls.demoplayback = false;
            Cls.state = ClientActivityState.Disconnected;

            if (Cls.timedemo)
            {
                FinishTimeDemo();
            }
        }

        /// <summary>
        /// CL_FinishTimeDemo
        /// </summary>
        static void FinishTimeDemo()
        {
            Cls.timedemo = false;

            // the first frame didn't count
            int frames = (Host.FrameCount - Cls.td_startframe) - 1;
            float time = (float)Host.RealTime - Cls.td_starttime;
            if (time == 0)
            {
                time = 1;
            }

            Con.Print("{0} frames {1:F5} seconds {2:F2} fps\n", frames, time, frames / time);
        }

        /// <summary>
        /// CL_WriteDemoMessage
        /// Dumps the current net message, prefixed by the length and view angles
        /// </summary>
        static void WriteDemoMessage ()
        {
	        int len = Common.LittleLong (Net.Message.Length);
            BinaryWriter writer = ((DisposableWrapper<BinaryWriter>)Cls.demofile).Object;
            writer.Write(len);
            writer.Write(Common.LittleFloat(Cl.viewangles.X));
            writer.Write(Common.LittleFloat(Cl.viewangles.Y));
            writer.Write(Common.LittleFloat(Cl.viewangles.Z));
            writer.Write(Net.Message.Data, 0, Net.Message.Length);
            writer.Flush();
        }
    }
}
