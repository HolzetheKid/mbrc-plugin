﻿using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using MusicBeePlugin.AndroidRemote.Entities;
using MusicBeePlugin.AndroidRemote.Error;

namespace MusicBeePlugin.AndroidRemote.Data
{
    /// <summary>
    /// Class CacheHelper.
    /// Is used to handle the library data and cover cache
    /// </summary>
    class CacheHelper
    {
        private const string CREATE_TABLE = "CREATE TABLE \"data\" (" +
                                            "\"_id\" integer primary key," +
                                            "\"hash\" TEXT," +
                                            "\"updated\" TEXT," +
                                            "\"filepath\" TEXT);";

        private const string ARTIST_IMAGE_TABLE = "CREATE TABLE \"artist_images\" (" +
                                                  "\"_id\" integer primary key," +
                                                  "\"artist\" TEXT," +
                                                  "\"updated\" TEXT," +
                                                  "\"url\" TEXT);";

        private const string PLAYLIST_TABLE = "create table \"playlists\" (" +
                                              "\"_id\" integer primary key," +
                                              "\"name\" text," +
                                              "\"path\" text," +
                                              "\"hash\" text)";

        private const string COVER_CACHE_TABLE = "create table \"covers\" (" +
                                                 "\"_id\" integer primary key," +
                                                 "\"coverhash\" text," +
                                                 "\"updated\" text," +
                                                 "\"album_id\" text)";

        private const string DB_NAME = @"\\cache.db";
        private string storagePath;
        private readonly string dbConnection;

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheHelper"/> class.
        /// </summary>
        /// <param name="storagePath">The storage path.</param>
        public CacheHelper(string storagePath)
        {
            this.storagePath = storagePath + DB_NAME;
            this.dbConnection = String.Format("Data Source={0}", this.storagePath);
            try
            {
                if (!File.Exists(this.storagePath))
                {
                    SQLiteConnection.CreateFile(this.storagePath);
                    using (SQLiteConnection mConnection = new SQLiteConnection(dbConnection))
                    using (SQLiteCommand mCommand = new SQLiteCommand(mConnection)) 
                    {
                        mConnection.Open();
                        mCommand.CommandText = CREATE_TABLE;
                        mCommand.ExecuteNonQuery();

                        mCommand.CommandText = ARTIST_IMAGE_TABLE;
                        mCommand.ExecuteNonQuery();

                        mCommand.CommandText = PLAYLIST_TABLE;
                        mCommand.ExecuteNonQuery();
                        
                        mCommand.CommandText = COVER_CACHE_TABLE;
                        mCommand.ExecuteNonQuery();

                        mConnection.Close();
                    }
                }
            }
            catch (Exception e)
            {
#if DEBUG
                ErrorHandler.LogError(e);
#endif
            }
        }

        public void CachePlaylists(List<Playlist> playlists)
        {
            try
            {
                using (var mConnection = new SQLiteConnection(dbConnection))
                {
                    mConnection.Open();
                    using (var mCommand = new SQLiteCommand(mConnection))
                    using (var mTransaction = mConnection.BeginTransaction())
                    {
                        mCommand.CommandText = "delete from playlists";
                        mCommand.ExecuteNonQuery();
                        mCommand.CommandText = "insert into playlists (name, path, hash) values (@name, @path, @hash);";
                        var nameParam = mCommand.CreateParameter();
                        var pathParam = mCommand.CreateParameter();
                        var hashParam = mCommand.CreateParameter();
                        nameParam.ParameterName = "@name";
                        pathParam.ParameterName = "@path";
                        hashParam.ParameterName = "@hash";
                        mCommand.Parameters.Add(nameParam);
                        mCommand.Parameters.Add(pathParam);
                        mCommand.Parameters.Add(hashParam);

                        foreach (var playlist in playlists)
                        {
                            nameParam.Value = playlist.name;
                            pathParam.Value = playlist.path;
                            hashParam.Value = playlist.hash;
                            mCommand.ExecuteNonQuery();
                        }
                        mTransaction.Commit();
                    }
                    mConnection.Close();
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                ErrorHandler.LogError(ex);
#endif
            }
        }

        public Playlist GetPlaylistByHash(string hash)
        {
            var playlist = new Playlist();
            try
            {
                using (var mConnection = new SQLiteConnection(dbConnection))
                {
                    mConnection.Open();
                    using (var mCommand = new SQLiteCommand(mConnection))
                    {
                        mCommand.CommandText = "select * from playlists where hash=@hash";
                        mCommand.Parameters.AddWithValue("@hash", hash);
                        SQLiteDataReader mReader = mCommand.ExecuteReader();
                        while (mReader.Read())
                        {
                            playlist.hash = mReader["hash"].ToString();
                            playlist.name = mReader["name"].ToString();
                            playlist.path = mReader["path"].ToString();
                        }
                        mReader.Close();
                    }
                    mConnection.Close();
                }
            }
            catch (Exception e)
            {
#if DEBUG
                ErrorHandler.LogError(e);
#endif
            }
            return playlist;
        }

        public void CreateCache(string[] filenames)
        {
            try
            {
                using (var mConnection = new SQLiteConnection(dbConnection))
                {
                    mConnection.Open();
                    using (var mCommand = new SQLiteCommand(mConnection))
                    using (var mTransaction = mConnection.BeginTransaction())
                    {
                        mCommand.CommandText = "insert into data (hash, filepath, updated) values (@hash, @filepath, @updated);";
                        var hashParam = mCommand.CreateParameter();
                        var fileParam = mCommand.CreateParameter();
                        var updatedParam = mCommand.CreateParameter();
                        hashParam.ParameterName = "@hash";
                        fileParam.ParameterName = "@filepath";
                        updatedParam.ParameterName = "@updated";
                        mCommand.Parameters.Add(hashParam);
                        mCommand.Parameters.Add(fileParam);
                        mCommand.Parameters.Add(updatedParam);

                        foreach (var filename in filenames)
                        {
                            hashParam.Value = Utilities.Utilities.Sha1Hash(filename);
                            fileParam.Value = filename;
                            updatedParam.Value = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ");
                            mCommand.ExecuteNonQuery();    
                        }
                        mTransaction.Commit();
                    }
                    mConnection.Close();
                }
            }
            catch (Exception e)
            {
#if DEBUG
                ErrorHandler.LogError(e);
#endif
            }
        }

        public void BuildImageCache(List<AlbumEntry> data)
        {
            try
            {
                using (var mConnection = new SQLiteConnection(dbConnection))
                {
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    mConnection.Open();
                    using (var mCommand = new SQLiteCommand(mConnection))
                    using (var mTransaction = mConnection.BeginTransaction())
                    {
                        mCommand.CommandText = "delete from covers";
                        mCommand.ExecuteNonQuery();

                        var cHashParam = mCommand.CreateParameter();
                        var albumIdParam = mCommand.CreateParameter();
                        var updated = mCommand.CreateParameter();
                        mCommand.CommandText = "insert into covers(coverhash, album_id, updated) values (@coverhash, @album_id, @updated);";
                        cHashParam.ParameterName = "@coverhash";
                        albumIdParam.ParameterName = "@album_id";
                        updated.ParameterName = "@updated";
                        mCommand.Parameters.Add(cHashParam);
                        mCommand.Parameters.Add(albumIdParam);
                        mCommand.Parameters.Add(updated);

                        foreach (var entry in data)
                        {
                            cHashParam.Value = entry.CoverHash;
                            albumIdParam.Value = entry.AlbumId;
                            updated.Value = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ");
                            mCommand.ExecuteNonQuery();   
                        }
                        mTransaction.Commit();
                    }
                    mConnection.Close();
                    sw.Stop();
                    Debug.WriteLine("Update transaction time {0}", sw.Elapsed);

                }
            }
            catch (Exception e)
            {
#if DEBUG
                ErrorHandler.LogError(e);
#endif
            }
        }

        /// <summary>
        /// Gets the cached files.
        /// </summary>
        /// <returns>List{LibraryData}.</returns>
        public List<LibraryData> GetCachedFiles()
        {
            List<LibraryData> data = new List<LibraryData>();
            try
            {
                using (SQLiteConnection mConnection = new SQLiteConnection(dbConnection))
                using (SQLiteCommand mCommand = new SQLiteCommand(mConnection))
                {
                    mConnection.Open();
                    mCommand.CommandText = "select * from data";
                    SQLiteDataReader mReader = mCommand.ExecuteReader();
                    while (mReader.Read())
                    {
                        var dataEntry = new LibraryData(mReader["hash"].ToString(), mReader["filepath"].ToString());
                        data.Add(dataEntry);
                    }
                    mReader.Close();
                    mConnection.Close();
                }
            }
            catch (Exception e)
            {
#if DEBUG
                ErrorHandler.LogError(e);
#endif
            }
            return data;
        }

        /// <summary>
        /// Gets the total number of covers available.
        /// </summary>
        /// <returns>System.Int32.</returns>
        public int GetCoversTotal()
        {
            var total = 0;
            try
            {
                using (var mConnection = new SQLiteConnection(dbConnection))
                using (var mCommand = new SQLiteCommand(mConnection))
                {
                    mConnection.Open();
                    mCommand.CommandText = "select count(*) as count from covers";
                    var mReader = mCommand.ExecuteReader();
                    while (mReader.Read())
                    {
                        total = int.Parse(mReader["count"].ToString());
                    }
                    mReader.Close();
                    mConnection.Close();
                }
            }
            catch (Exception e)
            {
#if DEBUG
                ErrorHandler.LogError(e);
#endif
            }
            return total;  
        }

        /// <summary>
        /// Retrieves a list of all the available AlbumEntries associated with covers.
        /// </summary>
        /// <param name="limit"></param>
        /// <param name="offset"></param>
        /// <returns>List{AlbumEntry}.</returns>
        public List<AlbumEntry> GetCoverHashes(int limit, int offset)
        {
            var data = new List<AlbumEntry>();
            try
            {
                using (var mConnection = new SQLiteConnection(dbConnection))
                using (var mCommand = new SQLiteCommand(mConnection))
                {
                    mConnection.Open();
                    mCommand.CommandText = "select * from covers limit @limit offset @offset";
                    var limitParam = mCommand.CreateParameter();
                    var offsetParam = mCommand.CreateParameter();
                    limitParam.ParameterName = "@limit";
                    offsetParam.ParameterName = "@offset";
                    limitParam.Value = limit;
                    offsetParam.Value = offset;
                    mCommand.Parameters.Add(limitParam);
                    mCommand.Parameters.Add(offsetParam);
                    var mReader = mCommand.ExecuteReader();
                    while (mReader.Read())
                    {
                        var entry = new AlbumEntry(mReader["album_id"].ToString())
                        {
                            CoverHash = mReader["coverhash"].ToString()
                        };

                        data.Add(entry);
                    }
                    mReader.Close();
                    mConnection.Close();
                }
            }
            catch (Exception e)
            {
#if DEBUG
                ErrorHandler.LogError(e);
#endif
            }
            return data;
        }

        /// <summary>
        /// Caches the artist URL along with the artist name in the database.
        /// </summary>
        /// <param name="artist">The artist.</param>
        /// <param name="url">The URL.</param>
        public void CacheArtistUrl(string artist, string url)
        {
            try
            {
                using (SQLiteConnection mConnection = new SQLiteConnection(dbConnection))
                using (SQLiteCommand mCommand = new SQLiteCommand(mConnection))
                {
                    mConnection.Open();
                    mCommand.CommandText = "insert into artist_images (artist, url) values (@artist, @url);";
                    mCommand.Parameters.AddWithValue("@artist", artist);
                    mCommand.Parameters.AddWithValue("@url", url);
                    mCommand.ExecuteNonQuery();
                    mConnection.Close();

                }
            }
            catch (Exception e)
            {
#if DEBUG
                ErrorHandler.LogError(e);
#endif
            }
        }

        /// <summary>
        /// Gets the entry by hash.
        /// </summary>
        /// <param name="hash">The hash sha1 hash of the entry.</param>
        /// <returns>LibraryData.</returns>
        public LibraryData GetEntryByHash(string hash)
        {
            LibraryData data = new LibraryData();
            try
            {
                using (SQLiteConnection mConnection = new SQLiteConnection(dbConnection))
                using (SQLiteCommand mCommand = new SQLiteCommand(mConnection))
                {
                    mConnection.Open();
                    mCommand.CommandText = "select * from data where hash=@hash";
                    mCommand.Parameters.AddWithValue("@hash", hash);
                    SQLiteDataReader mReader = mCommand.ExecuteReader();
                    while (mReader.Read())
                    {
                        data.CoverHash = mReader["coverhash"].ToString();
                        data.Hash = mReader["hash"].ToString();
                        data.Filepath = mReader["filepath"].ToString();
                    }
                    mReader.Close();
                    mConnection.Close();
                }
            }
            catch (Exception e)
            {
#if DEBUG
                ErrorHandler.LogError(e);
#endif
            }
            return data;
        }
    }
}
