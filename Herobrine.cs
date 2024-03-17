using MCGalaxy;
using MCGalaxy.Blocks;
using MCGalaxy.Bots;
using MCGalaxy.Events.ServerEvents;
using MCGalaxy.Events;
using MCGalaxy.Events.LevelEvents;
using MCGalaxy.Events.PlayerEvents;
using BlockID = System.UInt16;
using MCGalaxy.Network;
using MCGalaxy.Tasks;
using MCGalaxy.Commands;
using System;
using MCGalaxy.Maths;
namespace MCGalaxy
{
    public class Herobrine : Plugin
    {
        public override string name { get { return "Herobrine"; } }
        public override string MCGalaxy_Version { get { return "1.9.4.1"; } }
        public override string creator { get { return "Morgana"; } }

		//public bool LoadAtStartup = true;
		public bool HerobrineSpawned = false;
		public static SchedulerTask Task;
		
		public bool AllowGrief = true;
		
		public int CurrentHerobrineTask = 0; /*
			0 = Nothing
			1 = Stalk
			2 = anything (anything that uses this is responsible for turning it off)
		*/
		public override void Load(bool auto)
		{   
			Player[] players = PlayerInfo.Online.Items;
			HerobrineSpawned = false;
			OnBlockChangingEvent.Register(HandleBlockChanged, Priority.Low);
			OnPlayerConnectEvent.Register(HandlePlayerConnect, Priority.Low);
			Server.MainScheduler.QueueRepeat(DoHerobrineTick, null, TimeSpan.FromSeconds(1));
			int eventDelay = 40;
			Server.MainScheduler.QueueRepeat(DoHerobrineEvent, null, TimeSpan.FromSeconds(eventDelay));
			UpdateEnvAll();
		}
		public override void Unload(bool auto)
		{
			HerobrineSpawned = false;
			OnBlockChangingEvent.Unregister(HandleBlockChanged);
			OnPlayerConnectEvent.Unregister(HandlePlayerConnect);
			Server.MainScheduler.Cancel(Task);
			DestroyHerobrine();
		}
		PlayerBot heroEntity;
		int stalkTimeLeft = 0;
		int stalkTime = 120;
		int stalkDisappearDistance = 1200;
		public void DestroyHerobrine()
		{
			if (heroEntity == null)
			{
				return;
			}
			/*Player[] players = PlayerInfo.Online.Items;
			foreach (Player p in players)
			{
				Entities.Despawn(p, heroEntity);
			}*/
			PlayerBot.Remove(heroEntity);
			heroEntity = null;
		}			
		public void SpawnHerobrine(Level level, ushort x, ushort y, ushort z)
		{
			if (heroEntity != null)
			{
				return;
			}
			PlayerBot bot = new PlayerBot("Herobrine", level);
			bot.DisplayName = "";
			string skin = "herobrine";
			bot.SkinName = skin;
			bot.AIName = "stare";
			bot.id = 69;
			
			//+16 so that it's centered on the block instead of min corner
			Position pos = Position.FromFeet((int)(x*32) +16, (int)(y*32), (int)(z*32) +16);
			bot.SetInitialPos(pos);
			
			int yaw = 90;
			int pitch = 0;
			byte byteYaw = Orientation.DegreesToPacked(yaw);
			byte bytePitch = Orientation.DegreesToPacked(pitch);
			bot.SetYawPitch(byteYaw, bytePitch);
			
			heroEntity = bot;
			PlayerBot.Add(bot);
			/*
			Player[] players = PlayerInfo.Online.Items;
			foreach (Player p in players)
			{
				Entities.Spawn(p, bot);
			}*/
			
		}
		void DoStalk()
		{
			if (heroEntity == null)
			{
				CurrentHerobrineTask = 0;
				return;
			}
			if (stalkTimeLeft <= 0)
			{
				CurrentHerobrineTask = 0;
				DestroyHerobrine();
				return;
			}
			stalkTimeLeft--;
			Player[] players = PlayerInfo.Online.Items;
			int shortestDist = 2000000;
			Player selPlayer = null;
			foreach (Player p in players)
			{
				//Player closest = p;
				PlayerBot bot = heroEntity;
				int dx = p.Pos.X - bot.Pos.X, dy = p.Pos.Y - bot.Pos.Y, dz = p.Pos.Z - bot.Pos.Z;
				int playerDist = Math.Abs(dx) + Math.Abs(dy) + Math.Abs(dz);
				if (playerDist < shortestDist)
				{
					shortestDist = playerDist;
					selPlayer = p;
				}
				if (playerDist < stalkDisappearDistance) // if closer than this, disappear!
				{
					CurrentHerobrineTask = 0;
					DestroyHerobrine();
					return;
				}
			}
			if (selPlayer != null)
			{
				LookAtPlayer(selPlayer);
			}
		}
		void DoHerobrineTick(SchedulerTask task)
		{
			if (!HerobrineSpawned)
			{
				return;
			}
			Player[] playerlist = PlayerInfo.Online.Items;
		
			if (CurrentHerobrineTask == 1)
			{
				DoStalk();
			}
		}
		ushort FindGround(Level level,int x, int y, int z)
		{
			if (x > level.Width)
			{
				x = level.Width-1;
			}
			if (z > level.Length)
			{
				z = level.Length-1;
			}
			if (x < 0)
			{
				x = 0;
			}
			if (z < 0)
			{
				z = 0;
			}
			for (int i = level.Height-1; i >= 0; i--)
			{
				if (level.FastGetBlock((ushort)x, (ushort)i, (ushort)z) != 0)
				{
					return (ushort)(i + 1);
				}
			}
			return (ushort)y;
			
		}
		void LookAtPlayer(Player p)
		{
			if (heroEntity == null)
			{
				return;
			}
			PlayerBot bot = heroEntity;
			//p.Message("looking at you");
			int dstHeight = ModelInfo.CalcEyeHeight(bot);

			int dx = (p.Pos.X) - bot.Pos.X, dy = bot.Rot.RotY, dz = (p.Pos.Z) - bot.Pos.Z;
			Vec3F32 dir = new Vec3F32(dx, dy, dz);
			dir = Vec3F32.Normalise(dir);

			Orientation rot = bot.Rot;
			DirUtils.GetYawPitch(dir, out rot.RotY, out rot.HeadX);
			byte yaw = rot.RotY; byte pitch = rot.HeadX;
			bot.SetYawPitch(yaw, pitch);
			bot.Rot = rot;
		}
		void InitStalk()
		{
			Player[] players = PlayerInfo.Online.Items;
			Random rnd = new Random();
			Player selectedPlayer = players[rnd.Next(0, players.Length)];
			DestroyHerobrine();
			int rndX = rnd.Next(900, 1500);
			int rndZ = rnd.Next(900, 1500);
			if (rnd.Next(0,10) >= 5)
			{
				rndX = rndX * -1;
			}
			if (rnd.Next(0,10) >= 5)
			{
				rndZ = rndZ * -1;
			}
			int x = (selectedPlayer.Pos.X + rndX) / 32;
			int z = (selectedPlayer.Pos.Z + rndZ) / 32;
			if (x > selectedPlayer.level.Width)
			{
				x = selectedPlayer.level.Width;
			}
			if (z > selectedPlayer.level.Length)
			{
				z = selectedPlayer.level.Length;
			}
			if (x < 0)
			{
				x = 0;  
			}
			if (z < 0)
			{
				z = 0;
			}
			
			SpawnHerobrine(selectedPlayer.level, 
			(ushort)x,
			(ushort)FindGround(selectedPlayer.level,x/32 -16,selectedPlayer.Pos.Y/32,z/32 - 16),
			(ushort)z);
			LookAtPlayer(selectedPlayer);
			stalkTimeLeft = stalkTime;
		}
		void DoHerobrineEvent(SchedulerTask task)
		{
			if (!HerobrineSpawned)
			{
				return;
			}
			if (CurrentHerobrineTask != 0)
			{
				return;
			}
			Random rnd = new Random();
				
			int choice = rnd.Next(0, 5);
			
			if (choice == 1) // Stalk
			{
				stalkTimeLeft = stalkTime;
				InitStalk();
				CurrentHerobrineTask = 1;
				return;
			}
			Player[] players = PlayerInfo.Online.Items;
			if (choice == 2) // storm / clouds
			{
				if (sky.R == 150 && sky.G == 150 && sky.B == 170)
				{
					return;
				}
				//CurrentHerobrineTask = 2;
				SetSky(150,150,170);
				SetCloud(100,100,100);
				SetFog(100,100,100);
				Server.MainScheduler.QueueOnce( (SchedulerTask task2) => {
				SetSky(112, 160, 237);
				SetCloud(255,255,255);
				SetFog(255,255,255);
				//CurrentHerobrineTask = 0;
				}, null, TimeSpan.FromSeconds(1));
				Server.MainScheduler.QueueOnce( (SchedulerTask task2) => {
				SetSky(150,150,170);
				SetCloud(100,100,100);
				SetFog(100,100,100);
				//CurrentHerobrineTask = 0;
				}, null, TimeSpan.FromSeconds(2));
				Server.MainScheduler.QueueOnce( (SchedulerTask task2) => {
				SetSky(112, 160, 237);
				SetCloud(255,255,255);
				SetFog(255,255,255);
				//CurrentHerobrineTask = 0;
				}, null, TimeSpan.FromSeconds(3));
			}
			if (choice == 3 && AllowGrief) // cross in ground
			{
				CurrentHerobrineTask = 2;
				Level level = players[rnd.Next(0, players.Length)].level;
				int x = rnd.Next(0, level.Width);
				int z = rnd.Next(0, level.Length);
				int y = (int)FindGround(level, x, 20, z);
				//  cross in ground
				y = y-1;
				level.UpdateBlock(players[rnd.Next(0, players.Length)], (ushort)x,   (ushort)y, (ushort)z, 0);
				level.UpdateBlock(players[rnd.Next(0, players.Length)], (ushort)(x-1), (ushort)(y), (ushort)(z), 0);
				//level.UpdateBlock(players[rnd.Next(0, players.Length)], (ushort)(x-1), (ushort)(y+1), (ushort)(z), 4);
				level.UpdateBlock(players[rnd.Next(0, players.Length)], (ushort)(x-1), (ushort)(y), (ushort)(z+1), 0);
				level.UpdateBlock(players[rnd.Next(0, players.Length)], (ushort)(x-1), (ushort)(y), (ushort)(z-1), 0);
				level.UpdateBlock(players[rnd.Next(0, players.Length)], (ushort)(x-2), (ushort)(y), (ushort)(z), 0);
				level.UpdateBlock(players[rnd.Next(0, players.Length)], (ushort)(x-3), (ushort)(y), (ushort)(z), 0);
				Server.MainScheduler.QueueOnce( (SchedulerTask task2) => {
				CurrentHerobrineTask = 0;
				}, null, TimeSpan.FromSeconds(20));
				return;
			}
			
		}
		ColorDesc sky = new ColorDesc((byte)112, (byte)160, (byte)237);
	
		ColorDesc cloud = new ColorDesc((byte)255,(byte)255,(byte)255);

		ColorDesc fog = new ColorDesc((byte)255,(byte)255,(byte)255);
		void SetFog(byte r, byte g, byte b)
		{
			fog.R = r;
			fog.G = g;
			fog.B = b;
			UpdateEnvAll();
		}
		void SetSky(byte r, byte g, byte b)
		{
			sky.R = r;
			sky.G = g;
			sky.B = b;
			UpdateEnvAll();
		}
		void SetCloud(byte r, byte g, byte b)
		{
			cloud.R = r;
			cloud.G = g;
			cloud.B = b;
			UpdateEnvAll();
		}
		void UpdateEnv(Player pl)
		{
			  pl.Send(Packet.EnvColor(0, sky.R, sky.G, sky.B));
              pl.Send(Packet.EnvColor(1, cloud.R, cloud.G, cloud.B));
              pl.Send(Packet.EnvColor(2, fog.R, fog.G, fog.B));
		}
		void UpdateEnvAll()
		{
			Player[] players = PlayerInfo.Online.Items;
			foreach (Player pl in players)
            {
				UpdateEnv(pl);
			}
		}
		void HandlePlayerConnect(Player p)
        {
            UpdateEnv(p);
        }
		void HandleBlockChanged(Player p, ushort x, ushort y, ushort z, BlockID block, bool placing, ref bool cancel)
        {
			TrySummonHerobrine(p, x, y, z, block, placing);
        }
		
		void TrySummonHerobrine(Player p, ushort x, ushort y, ushort z, BlockID block, bool placing)
		{
			if (HerobrineSpawned)
			{
				return;
			}
			if (!placing)
			{
				return;
			}
			if (block != 54)
			{
				return;
			}


			if (p.level.GetBlock(x, (ushort)(y-1), z) != 62) // If magma underneath fire
			{
				return;
			}
			if (p.level.GetBlock(x, (ushort)(y-2), z) != 48) // If mossy block underneath fire
			{
				return;
			}
			if (p.level.GetBlock((ushort)(x-1), (ushort)(y-2), z) != 41) // If gold underneath fire
			{
				return;
			}
			if (p.level.GetBlock((ushort)(x+1), (ushort)(y-2), z) != 41) // If gold underneath fire
			{
				return;
			}
			if (p.level.GetBlock((ushort)(x-1), (ushort)(y-2), (ushort)(z-1)) != 41) // If gold underneath fire
			{
				return;
			}
			if (p.level.GetBlock((ushort)(x+1), (ushort)(y-2), (ushort)(z-1)) != 41) // If gold underneath fire
			{
				return;
			}
			if (p.level.GetBlock((ushort)(x-1), (ushort)(y-2), (ushort)(z+1)) != 41) // If gold underneath fire
			{
				return;
			}
			if (p.level.GetBlock((ushort)(x+1), (ushort)(y-2), (ushort)(z+1)) != 41) // If gold underneath fire
			{
				return;
			}
			if (p.level.GetBlock(x, (ushort)(y-2), (ushort)(z+1)) != 41) // If gold underneath fire
			{
				return;
			}
			if (p.level.GetBlock(x, (ushort)(y-2), (ushort)(z-1)) != 41) // If gold underneath fire
			{
				return;
			}
    
			/*if (p.level.Bots.Count >= Server.Config.MaxBotsPerLevel)
            {
                p.Message("Reached maximum number of bots allowed on this map.");
                return;
            }*/
			HerobrineSpawned = true;
			SpawnHerobrine(p.level, x,y,z);
			LookAtPlayer(p);
			SetSky(150,150,170);
			SetCloud(100,100,100);
			SetFog(100,100,100);
			Server.MainScheduler.QueueOnce( (SchedulerTask task) => {
				SetSky(112, 160, 237);
				SetCloud(255,255,255);
				SetFog(255,255,255);
				}, null, TimeSpan.FromSeconds(1));
			Server.MainScheduler.QueueOnce( (SchedulerTask task) => {
				SetSky(150,150,170);
				SetCloud(100,100,100);
				SetFog(100,100,100);
				}, null, TimeSpan.FromSeconds(2));
			Server.MainScheduler.QueueOnce( (SchedulerTask task) => {
				DestroyHerobrine();
				SetSky(112, 160, 237);
				SetCloud(255,255,255);
				SetFog(255,255,255);
				}, null, TimeSpan.FromSeconds(3));
            //
            //bot.Owner = p.truename;
		}
		
	}
	
}
