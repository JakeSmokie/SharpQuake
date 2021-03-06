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
using OpenTK;

// client.h

namespace SharpQuake
{
    static partial class Client
    {
        public const int SIGNONS = 4;	// signon messages to receive before connected
        public const int MAX_DLIGHTS = 32;
        public const int MAX_BEAMS = 24;
        public const int MAX_EFRAGS	= 640;
        public const int MAX_MAPSTRING = 2048;
        public const int MAX_DEMOS = 8;
        public const int MAX_DEMONAME = 16;
        public const int MAX_VISEDICTS = 256;
        
        const int MAX_TEMP_ENTITIES = 64;	// lightning bolts, etc
        const int MAX_STATIC_ENTITIES = 128;			// torches, etc


        static ClientStatic _Static = new ClientStatic();
        static ClientState _State = new ClientState();

        static EFrag[] _EFrags = new EFrag[MAX_EFRAGS]; // cl_efrags
        static Entity[] _Entities = new Entity[QDef.MAX_EDICTS]; // cl_entities
        static Entity[] _StaticEntities = new Entity[MAX_STATIC_ENTITIES]; // cl_static_entities
        static LightStyle[] _LightStyle = new LightStyle[QDef.MAX_LIGHTSTYLES]; // cl_lightstyle
        static DynamicLight[] _DLights = new DynamicLight[MAX_DLIGHTS]; // cl_dlights

        static Cvar _Name;// = { "_cl_name", "player", true };
        static Cvar _Color;// = { "_cl_color", "0", true };
        static Cvar _ShowNet;// = { "cl_shownet", "0" };	// can be 0, 1, or 2
        static Cvar _NoLerp;// = { "cl_nolerp", "0" };
        static Cvar _LookSpring;// = { "lookspring", "0", true };
        static Cvar _LookStrafe;// = { "lookstrafe", "0", true };
        static Cvar _Sensitivity;// = { "sensitivity", "3", true };
        static Cvar _MPitch;// = { "m_pitch", "0.022", true };
        static Cvar _MYaw;// = { "m_yaw", "0.022", true };
        static Cvar _MForward;// = { "m_forward", "1", true };
        static Cvar _MSide;// = { "m_side", "0.8", true };
        static Cvar _UpSpeed;// = { "cl_upspeed", "200" };
        static Cvar _ForwardSpeed;// = { "cl_forwardspeed", "200", true };
        static Cvar _BackSpeed;// = { "cl_backspeed", "200", true };
        static Cvar _SideSpeed;// = { "cl_sidespeed", "350" };
        static Cvar _MoveSpeedKey;// = { "cl_movespeedkey", "2.0" };
        static Cvar _YawSpeed;// = { "cl_yawspeed", "140" };
        static Cvar _PitchSpeed;// = { "cl_pitchspeed", "150" };
        static Cvar _AngleSpeedKey;// = { "cl_anglespeedkey", "1.5" };

        public static int NumVisEdicts; // cl_numvisedicts
        static Entity[] _VisEdicts = new Entity[MAX_VISEDICTS]; // cl_visedicts[MAX_VISEDICTS]

        
        public static ClientStatic Cls
        {
            get { return _Static; }
        }
        public static ClientState Cl
        {
            get { return _State; }
        }
        public static Entity[] Entities
        {
            get { return _Entities; }
        }
        /// <summary>
        /// cl_entities[cl.viewentity]
        /// Player model (visible when out of body)
        /// </summary>
        public static Entity ViewEntity
        {
            get { return _Entities[_State.viewentity]; }
        }
        /// <summary>
        /// cl.viewent
        /// Weapon model (only visible from inside body)
        /// </summary>
        public static Entity ViewEnt
        {
            get { return _State.viewent; }
        }
        public static float ForwardSpeed
        {
            get { return _ForwardSpeed.Value; }
        }
        public static bool LookSpring
        {
            get { return (_LookSpring.Value != 0); }
        }
        public static bool LookStrafe
        {
            get { return (_LookStrafe.Value != 0); }
        }
        public static DynamicLight[] DLights
        {
            get { return _DLights; }
        }
        public static LightStyle[] LightStyle
        {
            get { return _LightStyle; }
        }
        public static Entity[] VisEdicts
        {
            get { return _VisEdicts; }
        }
        public static float Sensitivity
        {
            get { return _Sensitivity.Value; }
        }
        public static float MSide
        {
            get { return _MSide.Value; }
        }
        public static float MYaw
        {
            get { return _MYaw.Value; }
        }
        public static float MPitch
        {
            get { return _MPitch.Value; }
        }
        public static float MForward
        {
            get { return _MForward.Value; }
        }
        public static string Name
        {
            get { return _Name.String; }
        }
        public static float Color
        {
            get { return _Color.Value; }
        }

    }

    struct LightStyle
    {
	    //public int length;
	    public string map; // [MAX_STYLESTRING];
    } // lightstyle_t;

    class Scoreboard
    {
	    public string name; //[MAX_SCOREBOARDNAME];
	    //public float entertime;
	    public int		frags;
	    public int		colors;			// two 4 bit fields
        public byte[] translations; // [VID_GRADES*256];

        public Scoreboard() => translations = new byte[Vid.VID_GRADES * 256];
    } // scoreboard_t;

    class CShift
    {
	    public int[] destcolor; // [3];
	    public int percent;		// 0-256

        public CShift() => destcolor = new int[3];

        public CShift(int[] destColor, int percent)
        {
            if (destColor.Length != 3)
            {
                throw new ArgumentException("destColor must have length of 3 elements!");
            }
            destcolor = destColor;
            this.percent = percent;
        }

        public void Clear()
        {
            destcolor[0] = 0;
            destcolor[1] = 0;
            destcolor[2] = 0;
            percent = 0;
        }
    } // cshift_t;

    static class ColorShift
    {
        public const int CSHIFT_CONTENTS = 0;
        public const int CSHIFT_DAMAGE = 1;
        public const int CSHIFT_BONUS = 2;
        public const int CSHIFT_POWERUP = 3;
        public const int NUM_CSHIFTS = 4;
    }

    class DynamicLight
    {
        public Vector3 origin;
        public float radius;
        public float die;				// stop lighting after this time
        public float decay;				// drop this each second
        public float minlight;			// don't add when contributing less
        public int key;

        public void Clear()
        {
            origin = Vector3.Zero;
            radius = 0;
            die = 0;
            decay = 0;
            minlight = 0;
            key = 0;
        }
    } //dlight_t;

    class Beam
    {
        public int entity;
        public Model model;
        public float endtime;
        public Vector3 start, end;

        public void Clear()
        {
            entity = 0;
            model = null;
            endtime = 0;
            start = Vector3.Zero;
            end = Vector3.Zero;
        }
    } // beam_t;

    enum ClientActivityState
    {
        Dedicated, 		// a dedicated server with no ability to start a client
        Disconnected, 	// full screen console with no connection
        Connected		// valid netcon, talking to a server
    } // cactive_t;

    //
    // the client_static_t structure is persistant through an arbitrary number
    // of server connections
    //
    class ClientStatic
    {
        public ClientActivityState state;

        // personalization data sent to server	
        public string mapstring; // [MAX_QPATH];
        public string spawnparms;//[MAX_MAPSTRING];	// to restart a level

        // demo loop control
        public int demonum;		// -1 = don't play demos
        public string[] demos; // [MAX_DEMOS][MAX_DEMONAME];		// when not playing

        // demo recording info must be here, because record is started before
        // entering a map (and clearing client_state_t)
        public bool demorecording;
        public bool demoplayback;
        public bool timedemo;
        public int forcetrack;			// -1 = use normal cd track
        public IDisposable demofile; // DisposableWrapper<BinaryReader|BinaryWriter> // FILE*
        public int td_lastframe;		// to meter out one message a frame
        public int td_startframe;		// host_framecount at start
        public float td_starttime;		// realtime at second frame of timedemo


        // connection information
        public int signon;			// 0 to SIGNONS
        public QSocket netcon; // qsocket_t	*netcon;
        public MessageWriter message; // sizebuf_t	message;		// writing buffer to send to server

        public ClientStatic()
        {
            demos = new string[Client.MAX_DEMOS];
            message = new MessageWriter(1024); // like in Client_Init()
        }
    } // client_static_t;
    

    //
    // the client_state_t structure is wiped completely at every
    // server signon
    //
    class ClientState
    {
        public int movemessages;	// since connecting to this server
        // throw out the first couple, so the player
        // doesn't accidentally do something the 
        // first frame
        public UserCommand cmd;			// last command sent to the server

        // information for local display
        public int[] stats; //[MAX_CL_STATS];	// health, etc
        public int items;			// inventory bit flags
        public float[] item_gettime; //[32];	// cl.time of aquiring item, for blinking
        public float faceanimtime;	// use anim frame if cl.time < this

        public CShift[] cshifts; //[NUM_CSHIFTS];	// color shifts for damage, powerups
        public CShift[] prev_cshifts; //[NUM_CSHIFTS];	// and content types

        // the client maintains its own idea of view angles, which are
        // sent to the server each frame.  The server sets punchangle when
        // the view is temporarliy offset, and an angle reset commands at the start
        // of each level and after teleporting.
        public Vector3[] mviewangles; //[2];	// during demo playback viewangles is lerped
        // between these
        public Vector3 viewangles;
        public Vector3[] mvelocity; //[2];	// update by server, used for lean+bob
        // (0 is newest)
        public Vector3 velocity;		// lerped between mvelocity[0] and [1]
        public Vector3 punchangle;		// temporary offset

        // pitch drifting vars
        public float idealpitch;
        public float pitchvel;
        public bool nodrift;
        public float driftmove;
        public double laststop;

        public float viewheight;
        public float crouch;			// local amount for smoothing stepups

        public bool paused;			// send over by server
        public bool onground;
        public bool inwater;

        public int intermission;	// don't change view angle, full screen, etc
        public int completed_time;	// latched at intermission start

        public double[] mtime; //[2];		// the timestamp of last two messages	
        public double time;			// clients view of time, should be between
        // servertime and oldservertime to generate
        // a lerp point for other data
        public double oldtime;		// previous cl.time, time-oldtime is used
        // to decay light values and smooth step ups


        public float last_received_message;	// (realtime) for net trouble icon

        //
        // information that is static for the entire time connected to a server
        //
        public Model[] model_precache; // [MAX_MODELS];
        public SFX[] sound_precache; // [MAX_SOUNDS];

        public string levelname; // char[40];	// for display on solo scoreboard
        public int viewentity;		// cl_entitites[cl.viewentity] = player
        public int maxclients;
        public int gametype;

        // refresh related state
        public Model worldmodel;	// cl_entitites[0].model
        public EFrag free_efrags; // first free efrag in list
        public int num_entities;	// held in cl_entities array
        public int num_statics;	// held in cl_staticentities array
        public Entity viewent;			// the gun model

        public int cdtrack, looptrack;	// cd audio

        // frag scoreboard
        public Scoreboard[] scores;		// [cl.maxclients]

        public ClientState()
        {
            stats = new int[QStats.MAX_CL_STATS];
            item_gettime = new float[32]; // ???????????

            cshifts = new CShift[ColorShift.NUM_CSHIFTS];
            for (int i = 0; i < ColorShift.NUM_CSHIFTS; i++)
            {
                cshifts[i] = new CShift();
            }

            prev_cshifts = new CShift[ColorShift.NUM_CSHIFTS];
            for (int i = 0; i < ColorShift.NUM_CSHIFTS; i++)
            {
                prev_cshifts[i] = new CShift();
            }

            mviewangles = new Vector3[2]; //??????
            mvelocity = new Vector3[2];
            mtime = new double[2];
            model_precache = new Model[QDef.MAX_MODELS];
            sound_precache = new SFX[QDef.MAX_SOUNDS];
            viewent = new Entity();
        }

        public bool HasItems(int item) => (items & item) == item;

        public void Clear()
        {
            movemessages = 0;
            cmd.Clear();
            Array.Clear(stats, 0, stats.Length);
            items = 0;
            Array.Clear(item_gettime, 0, item_gettime.Length);
            faceanimtime = 0;
            
            foreach (CShift cs in cshifts)
            {
                cs.Clear();
            }

            foreach (CShift cs in prev_cshifts)
            {
                cs.Clear();
            }

            mviewangles[0] = Vector3.Zero;
            mviewangles[1] = Vector3.Zero;
            viewangles = Vector3.Zero;
            mvelocity[0] = Vector3.Zero;
            mvelocity[1] = Vector3.Zero;
            velocity = Vector3.Zero;
            punchangle = Vector3.Zero;

            idealpitch = 0;
            pitchvel = 0;
            nodrift = false;
            driftmove = 0;
            laststop = 0;

            viewheight = 0;
            crouch = 0;

            paused = false;
            onground = false;
            inwater = false;

            intermission = 0;
            completed_time = 0;

            mtime[0] = 0;
            mtime[1] = 0;
            time = 0;
            oldtime = 0;
            last_received_message = 0;

            Array.Clear(model_precache, 0, model_precache.Length);
            Array.Clear(sound_precache, 0, sound_precache.Length);

            levelname = null;
            viewentity = 0;
            maxclients = 0;
            gametype = 0;

            worldmodel = null;
            free_efrags = null;
            num_entities = 0;
            num_statics = 0;
            viewent.Clear();

            cdtrack = 0;
            looptrack = 0;

            scores = null;
        }
    } //client_state_t;

    struct UserCommand
    {
        public Vector3 viewangles;

        // intended velocities
	    public float forwardmove;
	    public float sidemove;
	    public float upmove;

        public void Clear()
        {
            viewangles = Vector3.Zero;
            forwardmove = 0;
            sidemove = 0;
            upmove = 0;
        }
    }// usercmd_t;

    //
    // cl_input
    //
    struct KeyButton
    {
	    public int down0, down1;		// key nums holding it down
	    public int state;			// low bit is down state

        public bool IsDown
        {
            get { return (state & 1) != 0; }
        }
    } // kbutton_t;
}
