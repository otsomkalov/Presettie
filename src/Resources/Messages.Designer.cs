﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Resources {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    public class Messages {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Messages() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Resources.Messages", typeof(Messages).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to 
        ///*Q:* Why tracks, that I&apos;ve added to playlist using Spotify, are not respected by the bot?
        ///*A:* Bot uses caching in order not to bomb Spotify API with requests. Your playlist tracks is cached once a week. For the moment there is no possibility to force cache clean.
        ///*Q:* Why sometimes it takes minutes to generate my playlist?
        ///*A:* When cached playlist tracks expires, we need to fetch tracks from Spotify API, which takes some time.
        ///        .
        /// </summary>
        public static string FAQ {
            get {
                return ResourceManager.GetString("FAQ", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to 
        ///*Your first preset*
        ///In this guide, we are going to prepare your first preset that will do the next:
        ///1. Include your liked tracks
        ///2. Exclude already listened tracks
        ///3. Get recommendations
        ///4. Save to targeted playlist
        ///Steps would be the next:
        ///1. Login to the bot via Spotify
        ///2. Click &quot;➕ Create preset&quot; keyboard button
        ///3. Enter the name for your preset. You may use something like &quot;Recommendations.gen&quot;
        ///4. You&apos;ll see the info about created preset. Lets configure it:
        ///    1. By default, preset includes  [rest of string was truncated]&quot;;.
        /// </summary>
        public static string Guide {
            get {
                return ResourceManager.GetString("Guide", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to 
        ///Bot can generate playlists from the other playlists or liked tracks and get Spotify recommendations for them
        ///
        ///The idea is based on concept called *presets*: set of *included playlists*, *excluded playlists* and *targeted playlists*:
        ///1. *Targeted playlists* are your destination playlists for generated tracks
        ///2. *Included playlists* are the sources for tracks
        ///3. *Excluded playlists* tracks are removed from both recommended and included tracks
        ///
        ///Additionally, *preset* has the next options:
        ///1. Include/ [rest of string was truncated]&quot;;.
        /// </summary>
        public static string Help {
            get {
                return ResourceManager.GetString("Help", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to 
        ///*Name*: {0}
        ///*Tracks*: {1}
        ///*Only liked*: {2}
        ///        .
        /// </summary>
        public static string IncludedPlaylistDetails {
            get {
                return ResourceManager.GetString("IncludedPlaylistDetails", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to ➖ Liked tracks are excluded.
        /// </summary>
        public static string LikedTracksExcluded {
            get {
                return ResourceManager.GetString("LikedTracksExcluded", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to ❌ Liked tracks are ignored.
        /// </summary>
        public static string LikedTracksIgnored {
            get {
                return ResourceManager.GetString("LikedTracksIgnored", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to ➕ Liked tracks are included.
        /// </summary>
        public static string LikedTracksIncluded {
            get {
                return ResourceManager.GetString("LikedTracksIncluded", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Seems like my access to your Spotify library has expired. Please, re-login using link below.
        /// </summary>
        public static string LoginToSpotify {
            get {
                return ResourceManager.GetString("LoginToSpotify", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Cannot parse provided playlist id: {0}.
        /// </summary>
        public static string PlaylistIdCannotBeParsed {
            get {
                return ResourceManager.GetString("PlaylistIdCannotBeParsed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You don&apos;t have permission to modify this playlist.
        /// </summary>
        public static string PlaylistIsReadonly {
            get {
                return ResourceManager.GetString("PlaylistIsReadonly", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Playlist with id {0} not found is Spotify!.
        /// </summary>
        public static string PlaylistNotFoundInSpotify {
            get {
                return ResourceManager.GetString("PlaylistNotFoundInSpotify", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Preset name: *{0}*
        ///Preset settings:
        ///*Include liked tracks* - {1}
        ///*Recommendations* - {2}
        ///*Unique artists* - {3}
        ///*Playlist size* - {4}.
        /// </summary>
        public static string PresetInfo {
            get {
                return ResourceManager.GetString("PresetInfo", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Playlist size should be a number!.
        /// </summary>
        public static string PresetSizeNotANumber {
            get {
                return ResourceManager.GetString("PresetSizeNotANumber", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to New playlist size is set!.
        /// </summary>
        public static string PresetSizeSet {
            get {
                return ResourceManager.GetString("PresetSizeSet", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Playlist size should be lower than 10000!.
        /// </summary>
        public static string PresetSizeTooBig {
            get {
                return ResourceManager.GetString("PresetSizeTooBig", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Playlist size should be greater than 0!.
        /// </summary>
        public static string PresetSizeTooSmall {
            get {
                return ResourceManager.GetString("PresetSizeTooSmall", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to 
        ///Bot needs you to log in into Spotify in order to perform some actions, including playlist generation.
        ///Bot request 3 permissions from you in Spotify: modify public playlists, modify private playlists, read user library (liked tracks).
        ///        .
        /// </summary>
        public static string Privacy {
            get {
                return ResourceManager.GetString("Privacy", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to ➖ Recommendations are disabled.
        /// </summary>
        public static string RecommendationsDisabled {
            get {
                return ResourceManager.GetString("RecommendationsDisabled", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to ➕ Recommendations are enabled.
        /// </summary>
        public static string RecommendationsEnabled {
            get {
                return ResourceManager.GetString("RecommendationsEnabled", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Send id/url of Spotify playlist to exclude it tracks from generation.
        /// </summary>
        public static string SendExcludedPlaylist {
            get {
                return ResourceManager.GetString("SendExcludedPlaylist", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Send id/url of Spotify playlist to include it tracks into generation.
        /// </summary>
        public static string SendIncludedPlaylist {
            get {
                return ResourceManager.GetString("SendIncludedPlaylist", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Send new preset name in reply to this message.
        /// </summary>
        public static string SendPresetName {
            get {
                return ResourceManager.GetString("SendPresetName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Send me new playlist size in reply to this message.
        /// </summary>
        public static string SendPresetSize {
            get {
                return ResourceManager.GetString("SendPresetSize", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Send id/url of Spotify playlist to save generated tracks into.
        /// </summary>
        public static string SendTargetedPlaylist {
            get {
                return ResourceManager.GetString("SendTargetedPlaylist", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to 👥 Non-unique artists.
        /// </summary>
        public static string UniqueArtistsDisabled {
            get {
                return ResourceManager.GetString("UniqueArtistsDisabled", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to 👤 Unique artists.
        /// </summary>
        public static string UniqueArtistsEnabled {
            get {
                return ResourceManager.GetString("UniqueArtistsEnabled", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Updated.
        /// </summary>
        public static string Updated {
            get {
                return ResourceManager.GetString("Updated", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to 
        ///Hello!
        ///
        ///To get more information about the bot use /help command.
        ///
        ///Use the link below to log in using Spotify and generate playlists.
        ///        .
        /// </summary>
        public static string Welcome {
            get {
                return ResourceManager.GetString("Welcome", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Command data should be positive number indicates playlist size.
        /// </summary>
        public static string WrongPresetSize {
            get {
                return ResourceManager.GetString("WrongPresetSize", resourceCulture);
            }
        }
    }
}
