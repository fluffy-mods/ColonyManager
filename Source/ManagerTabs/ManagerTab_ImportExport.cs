//// Karel Kroeze
//// ManagerTab_ImportExport.cs
//// 2016-12-09

//using RimWorld;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Reflection;
//using System.Text;
//using UnityEngine;
//using Verse;

//namespace FluffyManager
//{
//    internal class ManagerTab_ImportExport : ManagerTab
//    {
//        #region Fields

//        private string _folder = "";

//        private float _iconSize = 24f;

//        private List<Pair<string, int>> _jobCounts;

//        private JobStack _jobStackIO;

//        private float _loadAreaRatio = .6f;

//        private float _margin = Utilities.Margin;

//        private float _rowHeight = 30f;

//        private string _saveExtension = ".rwm";

//        private List<SaveFileInfo> _saveFiles;

//        private string _saveName = "";

//        private string _saveNameBase = "ManagerSave_";

//        #endregion Fields

//        #region Constructors

//        public ManagerTab_ImportExport( Manager manager ) : base( manager )
//        {
//            _jobStackIO = new JobStack( manager );
//        }

//        #endregion Constructors


//        #region Properties

//        public override Texture2D Icon
//        {
//            get { return Resources.IconImportExport; }
//        }

//        public override IconAreas IconArea
//        {
//            get { return IconAreas.Right; }
//        }

//        public override string Label
//        {
//            get { return "FM.ImportExport".Translate(); }
//        }

//        public override ManagerJob Selected
//        {
//            // not used.
//            get { return null; }
//            set { }
//        }

//        #endregion Properties


//        #region Methods

//        public override void DoWindowContents( Rect canvas )
//        {
//            var loadRect = new Rect( 0f, 0f, ( canvas.width - _margin ) * _loadAreaRatio, canvas.height );
//            var saveRect = new Rect( loadRect.xMax + _margin, 0f, canvas.width - _margin - loadRect.width,
//                                     canvas.height );
//            Widgets.DrawMenuSection( loadRect );
//            Widgets.DrawMenuSection( saveRect );

//            DrawLoadSection( loadRect );
//            DrawSaveSection( saveRect );
//        }

//        public override void PreOpen()
//        {
//            // set save location
//            _folder = GetSaveLocation();

//            // variable stuff
//            Refresh();
//        }

//        public void Refresh()
//        {
//            // List of current job counts
//            _jobCounts = ( from job in Manager.For( manager ).JobStack.FullStack()
//                           group job by job.Tab.Label
//                           into jobs
//                           select new Pair<string, int>( jobs.Key, jobs.Count() ) ).ToList();

//            // fetch the list of saved jobs
//            _saveFiles = GetSavedFilesList();

//            // set a valid default name
//            _saveName = DefaultSaveName();
//        }

//        private string DefaultSaveName()
//        {
//            // keep adding 1 until we have a new name.
//            var i = 1;
//            string name = _saveNameBase + i;
//            while ( SaveExists( name ) )
//            {
//                i++;
//                name = _saveNameBase + i;
//            }

//            return name;
//        }

//        private void DoExport( string name )
//        {
//            try
//            {
//                try
//                {
//                    Scribe.InitWriting( FilePath( name ), "ManagerJobs" );
//                }
//                catch ( Exception ex )
//                {
//                    GenUI.ErrorDialog( "ProblemSavingFile".Translate( ex.ToString() ) );
//                    throw;
//                }

//                ScribeMetaHeaderUtility.WriteMetaHeader();

//                _jobStackIO = Manager.For( manager ).JobStack;
//                Scribe_Deep.Look( ref _jobStackIO, "JobStack" );
//            }
//            catch ( Exception ex2 )
//            {
//                Log.Error( "Exception while saving jobstack: " + ex2 );
//            }
//            finally
//            {
//                Scribe.FinalizeWriting();
//                Messages.Message( "FM.JobsExported".Translate( _jobStackIO.FullStack().Count ), MessageSound.Standard );
//                Refresh();
//            }
//        }

//        private void DoImport( SaveFileInfo file )
//        {
//            try
//            {
//                // load stuff
//                Scribe.InitLoading( _folder + "/" + file.FileInfo.Name );
//                Manager.LoadSaveMode = Manager.Modes.ImportExport;
//                ScribeMetaHeaderUtility.LoadGameDataHeader( ScribeMetaHeaderUtility.ScribeHeaderMode.Map, false );
//                Scribe.EnterNode( "JobStack" );
//                _jobStackIO.ExposeData();
//                Scribe.ExitNode();
//                Scribe.FinalizeLoading();

//                // resolve crossreferences
//                // these are registered during the loading stage, and cleared afterwards
//                // will most definitely give errors/warnings on crossgame imports
//                CrossRefResolver.ResolveAllCrossReferences();

//                // replace the old jobstack
//                Manager.For( manager ).NewJobStack( _jobStackIO );

//                // remove invalid jobs
//                var invalid = 0;
//                foreach ( ManagerJob job in Manager.For( manager ).JobStack.FullStack() )
//                {
//                    if ( !job.IsValid )
//                    {
//                        invalid++;
//                        job.Delete( false );
//                    }
//                }

//                // provide some feedback on failed import(s)
//                // if debug is enabled the screen will also pop up with reference errors.
//                if ( invalid > 0 )
//                {
//                    Messages.Message( "FM.InvalidJobsDeleted".Translate( invalid ), MessageSound.SeriousAlert );
//                }
//            }
//            catch ( Exception e )
//            {
//                Log.Error( "Exception while loading jobstack: " + e );
//            }
//            finally
//            {
//                // done?
//                Scribe.mode = LoadSaveMode.Inactive;
//                Manager.LoadSaveMode = Manager.Modes.Normal;
//                Messages.Message( "FM.JobsImported".Translate( _jobStackIO.FullStack().Count ), MessageSound.Standard );
//                Refresh();
//            }
//        }

//        private void DrawFileEntry( Rect rect, SaveFileInfo file )
//        {
//            GUI.BeginGroup( rect );

//            // set up rects
//            Rect nameRect = rect.AtZero();
//            nameRect.width -= 200f + _iconSize + 4 * _margin;
//            nameRect.xMin += _margin;
//            var timeRect = new Rect( nameRect.xMax + _margin, 0f, 100f, rect.height );
//            var buttonRect = new Rect( timeRect.xMax + _margin, 1f, 100f, rect.height - 2f );
//            var deleteRect = new Rect( buttonRect.xMax + _margin, ( rect.height - _iconSize ) / 2, _iconSize, _iconSize );

//            // name
//            Text.Anchor = TextAnchor.MiddleLeft;
//            Widgets.Label( nameRect, Path.GetFileNameWithoutExtension( file.FileInfo.Name ) );
//            Text.Anchor = TextAnchor.UpperLeft;

//            // timestamp
//            GUI.color = Color.gray;
//            Dialog_FileList.DrawDateAndVersion( file, timeRect );
//            Text.Font = GameFont.Small;
//            GUI.color = Color.white;

//            // load button
//            if ( Widgets.ButtonText( buttonRect, "FM.Import".Translate() ) )
//            {
//                TryImport( file );
//            }

//            // delete button
//            if ( Widgets.ButtonImage( deleteRect, Resources.DeleteX ) )
//            {
//                Find.WindowStack.Add( new Dialog_Confirm( "ConfirmDelete".Translate( file.FileInfo.Name ), delegate
//                                                                                                               {
//                                                                                                                   file
//                                                                                                                       .FileInfo
//                                                                                                                       .Delete
//                                                                                                                       ();
//                                                                                                                   Refresh
//                                                                                                                       ();
//                                                                                                               }, true ) );
//            }

//            GUI.EndGroup();
//        }

//        private void DrawLoadSection( Rect rect )
//        {
//            if ( _saveFiles.NullOrEmpty() )
//            {
//                // no saves found.
//                GUI.color = Color.gray;
//                Text.Anchor = TextAnchor.MiddleCenter;
//                Widgets.Label( rect, "FM.NoSaves".Translate() );
//                Text.Anchor = TextAnchor.UpperLeft;
//                GUI.color = Color.white;
//            }
//            else
//            {
//                GUI.BeginGroup( rect );
//                Vector2 cur = Vector2.zero;
//                try
//                {
//                    var i = 1;
//                    foreach ( SaveFileInfo file in _saveFiles )
//                    {
//                        var row = new Rect( 0f, cur.y, rect.width, _rowHeight );
//                        if ( i++ % 2 == 0 )
//                        {
//                            Widgets.DrawAltRect( row );
//                        }
//                        DrawFileEntry( row, file );
//                        cur.y += _rowHeight;
//                    }
//                }
//                finally
//                {
//                    // make sure it gets ended even if something fails.
//                    GUI.EndGroup();
//                }
//            }
//        }

//        private void DrawSaveSection( Rect rect )
//        {
//            var infoRect = new Rect( rect.ContractedBy( _margin ) );
//            infoRect.height -= 30f + _margin;
//            var nameRect = new Rect( rect.xMin + _margin, infoRect.yMax, ( rect.width - 3 * _margin ) / 2, 30f );
//            var buttonRect = new Rect( nameRect.xMax + _margin, infoRect.yMax, nameRect.width, 30f );

//            var info = new StringBuilder();
//            info.AppendLine( "FM.CurrentJobs".Translate() );
//            foreach ( Pair<string, int> jobCount in _jobCounts )
//            {
//                info.AppendLine( jobCount.First + ": " + jobCount.Second );
//            }

//            Widgets.Label( infoRect, info.ToString() );
//            GUI.SetNextControlName( "ManagerJobsNameField" );
//            string name = Widgets.TextField( nameRect, _saveName );
//            if ( GenText.IsValidFilename( name ) )
//            {
//                _saveName = name;
//            }
//            if ( GenText.IsValidFilename( _saveName ) )
//            {
//                if ( Widgets.ButtonText( buttonRect, "FM.Export".Translate() ) )
//                {
//                    TryExport( _saveName );
//                }
//            }
//            else
//            {
//                GUI.color = Color.gray;
//                Text.Anchor = TextAnchor.MiddleCenter;
//                Widgets.DrawBox( buttonRect );
//                Widgets.Label( buttonRect, "FM.InvalidName".Translate() );
//                GUI.color = Color.white;
//                Text.Anchor = TextAnchor.UpperLeft;
//            }
//        }

//        private string FilePath( string name )
//        {
//            return _folder + "/" + name + _saveExtension;
//        }

//        private List<SaveFileInfo> GetSavedFilesList()
//        {
//            var directoryInfo = new DirectoryInfo( _folder );

//            // raw files
//            IOrderedEnumerable<FileInfo> files = from f in directoryInfo.GetFiles()
//                                                 where f.Extension == _saveExtension
//                                                 orderby f.LastWriteTime descending
//                                                 select f;

//            // convert to RW save files - mostly for the headers
//            var saves = new List<SaveFileInfo>();
//            foreach ( FileInfo current in files )
//            {
//                try
//                {
//                    saves.Add( new SaveFileInfo( current ) );
//                }
//                catch ( Exception ex )
//                {
//                    Log.Error( "Exception loading " + current.Name + ": " + ex );
//                }
//            }

//            return saves;
//        }

//        private string GetSaveLocation()
//        {
//            // Get method "FolderUnderSaveData" from GenFilePaths, which is private (NonPublic) and static.
//            MethodInfo Folder = typeof( GenFilePaths ).GetMethod( "FolderUnderSaveData",
//                                                                  BindingFlags.NonPublic |
//                                                                  BindingFlags.Static );
//            if ( Folder == null )
//            {
//                throw new Exception( "FolderUnderSaveData [reflection] is null" );
//            }

//            // Call "FolderUnderSaveData" from null parameter, since this is a static method.
//            return (string)Folder.Invoke( null, new object[] { "ManagerJobs" } );
//        }

//        private bool SaveExists( string name )
//        {
//            return _saveFiles.Any( save => save.FileInfo.Name == name + _saveExtension );
//        }

//        private void TryExport( string name )
//        {
//            // if it exists, confirm overwrite
//            if ( SaveExists( name ) )
//            {
//                Find.WindowStack.Add( new Dialog_Confirm( "FM.ConfirmOverwrite".Translate( name ),
//                                                          delegate
//                                                          { DoExport( name ); }, true ) );
//            }
//            else
//            {
//                DoExport( name );
//            }
//        }

//        private void TryImport( SaveFileInfo file )
//        {
//            DoImport( file );
//        }

//        #endregion Methods
//    }
//}

