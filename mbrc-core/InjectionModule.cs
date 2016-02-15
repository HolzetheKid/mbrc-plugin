﻿namespace MusicBeePlugin
{
    using MusicBeePlugin.AndroidRemote.Data;
    using MusicBeePlugin.AndroidRemote.Events;
    using MusicBeePlugin.AndroidRemote.Model;
    using MusicBeePlugin.AndroidRemote.Networking;
    using MusicBeePlugin.AndroidRemote.Persistence;
    using MusicBeePlugin.ApiAdapters;
    using MusicBeePlugin.Modules;
    using MusicBeePlugin.Repository;

    using MusicBeeRemoteCore.Interfaces;

    using Ninject.Modules;

    internal class InjectionModule : NinjectModule
    {
        public static string StoragePath;

        private IBindingProvider provider;

        public InjectionModule(IBindingProvider provider)
        {
            this.provider = provider;
        }

        public override void Load()
        {
            this.Bind<IPlayerApiAdapter>().ToMethod(context => this.provider.PlayerApi).InSingletonScope();
            this.Bind<IPlaylistApiAdapter>().ToMethod(context => this.provider.PlaylistApi).InSingletonScope();
            this.Bind<ITrackApiAdapter>().ToMethod(context => this.provider.TrackApi).InSingletonScope();
            this.Bind<ILibraryApiAdapter>().ToMethod(context => this.provider.LibraryApi).InSingletonScope();
            this.Bind<INowPlayingApiAdapter>().ToMethod(context => this.provider.NowPlayingApi).InSingletonScope();
           
            this.Bind<DatabaseProvider>().ToSelf().WithConstructorArgument("storagePath", StoragePath);
            this.Bind<LibraryModule>().ToSelf().InSingletonScope();
            this.Bind<NowPlayingModule>().ToSelf().InSingletonScope();
            this.Bind<PlayerModule>().ToSelf().InSingletonScope();
            this.Bind<PlaylistModule>().ToSelf().InSingletonScope();
            this.Bind<TrackModule>().ToSelf().InSingletonScope();
            this.Bind<LyricCoverModel>().ToSelf().InSingletonScope();
            this.Bind<EventBus>().ToSelf().InSingletonScope();
            this.Bind<PersistenceController>()
                .ToSelf()
                .InSingletonScope()
                .WithConstructorArgument("storagePath", StoragePath);
            this.Bind<SocketServer>().ToSelf().InSingletonScope();

            this.Bind<IGenreRepository>().To<GenreRepository>().InSingletonScope();
            this.Bind<IAlbumRepository>().To<AlbumRepository>().InSingletonScope();
            this.Bind<IArtistRepository>().To<ArtistRepository>().InSingletonScope();
            this.Bind<ITrackRepository>().To<TrackRepository>().InSingletonScope();
            this.Bind<IPlaylistRepository>().To<PlaylistRepository>().InSingletonScope();
            this.Bind<ICoverRepository>().To<CoverRepository>().InSingletonScope();
            this.Bind<IPlaylistTrackRepository>().To<PlaylistTrackRepository>().InSingletonScope();
            this.Bind<IPlaylistTrackInfoRepository>().To<PlaylistTrackInfoRepository>().InSingletonScope();
        }
    }
}