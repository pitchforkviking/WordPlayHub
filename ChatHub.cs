using Microsoft.AspNetCore.SignalR;
using System.Collections;
using System.Linq;
using System.Collections.Generic;

namespace WordPlay
{   
    public class Player
    {
        public string name { get; set; }

        public int score { get; set; }

        public char[] deck { get { return new char[2];} set{} }

        public char[] board { get { return new char[2];} set{} }

        public char[] borrow { get { return new char[2];} set{} }
    }
    public class ChatHub : Hub
    {
        //private static NameValueCollection members = new NameValueCollection(20);
        private static List<KeyValuePair<string, Player>> members = new List<KeyValuePair<string, Player>>();

        public void Delete(string key)
        {
            members.RemoveAll(item => item.Key.Equals(key));
        }

        public void Host(string key, int mode, string name)
        {
            Groups.AddAsync(Context.ConnectionId, key);
            Player player = new Player();
            player.name = name; 
            members.Add(new KeyValuePair<string, Player>(key, player));
            var lookup = members.ToLookup(kvp => kvp.Key, kvp => kvp.Value);

            int count = lookup[key].Count();
            
            string message = string.Format("{0} Joined ({1}/{2})", name, count, mode);
            
            int play = count - 1;
            //Clients.Group(key).InvokeAsync("fjoin", message, play);
            Clients.Client(Context.ConnectionId).InvokeAsync("fjoin", message, play);
        }

        public void Sync(string key, object players, int pass)
        { 
            ++pass;
            Clients.Group(key).InvokeAsync("fsync", players, pass);
        }

        public void List()
        {
            var lookup = members.ToLookup(kvp => kvp.Key, kvp => kvp.Value);
            var keys = new string[lookup.Select(k => k.Key).Count()];
            int index = 0;
            foreach(var item in lookup.Select(k => k.Key))
            {
                keys[index] = item;
                ++index;
            }
            Clients.All.InvokeAsync("flist", keys.ToList());            
        }

        public void Join(string key, int mode, string name)
        {       
            Groups.AddAsync(Context.ConnectionId, key);              
            
            var lookup = members.ToLookup(kvp => kvp.Key, kvp => kvp.Value);

            int count = lookup[key].Count();
            
            if(count < mode){
                
                Player player = new Player();
                player.name = name; 
                members.Add(new KeyValuePair<string, Player>(key, player));

                lookup = null;
                lookup = members.ToLookup(kvp => kvp.Key, kvp => kvp.Value);

                count = lookup[key].Count();
                string message = string.Format("{0} Joined ({1}/{2})", name, count, mode);

                int play = count - 1;
                //Clients.Group(key).InvokeAsync("fjoin", message, play);                
                Clients.Client(Context.ConnectionId).InvokeAsync("fjoin", message, play);

                if(count == mode){   

                    var players = new Player[mode];                 
                    int index = 0;
                    foreach (var value in lookup[key])
                    {
                        players[index] = value;
                        ++index;
                    }  

                    //var result = players.ToArray();

                    Clients.Group(key).InvokeAsync("fbegin", players.ToList());
                }
            }            
        }

        public void Broadcast(string key, string message)
        {
            Clients.Group(key).InvokeAsync("fbroadcast", message);            
        }
    }
}