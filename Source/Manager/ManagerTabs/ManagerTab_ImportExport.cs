using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using UnityEngine;
using RimWorld;
using Verse;
using System.Reflection;

namespace FM
{
    class ManagerTab_ImportExport : ManagerTab
    {
        private float _margin = Manager.Margin;
        private float _loadAreaRatio = .6f;
        private string _folder = "";
        private List<Pair<string, int>> _jobCounts;
        private string _saveName = "";
        private string _saveNameBase = "ManagerSave_";
        private string _saveExtension = ".rwm";
        private JobStack _jobStackIO = new JobStack();
        private List<SaveFileInfo> _saveFiles;
        private float _rowHeight = 30f;
        private float _iconSize = 24f;
        
        public override void PreOpen()
        {
            // set save location
            _folder = GetSaveLocation();

            // variable stuff
            Refresh();
        }

        public void Refresh()
        {
            // List of current job counts
            _jobCounts = ( from job in Manager.Get.JobStack.FullStack
                           group job by job.Tab.Label into jobs
                           select new Pair<string, int>( jobs.Key, jobs.Count() ) ).ToList();

            // fetch the list of saved jobs
            _saveFiles = GetSavedFilesList();

            // set a valid default name
            _saveName = DefaultSaveName();
        }

        private List<SaveFileInfo> GetSavedFilesList()
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(_folder);

            // raw files
            var files = from f in directoryInfo.GetFiles()
                        where f.Extension == _saveExtension
                        orderby f.LastWriteTime descending
                        select f;

            // convert to RW save files - mostly for the headers
            List < SaveFileInfo > saves = new List<SaveFileInfo>();
            foreach( FileInfo current in files )
            {
                try
                {
                    saves.Add( new SaveFileInfo( current ) );
                }
                catch( Exception ex )
                {
                    Log.Error( "Exception loading " + current.Name + ": " + ex.ToString() );
                }
            }

            return saves;
        }

        private string GetSaveLocation()
        {
            // Get method "FolderUnderSaveData" from GenFilePaths, which is private (NonPublic) and static.
            var Folder = typeof(GenFilePaths).GetMethod( "FolderUnderSaveData",
                                                                           BindingFlags.NonPublic |
                                                                           BindingFlags.Static );
            if( Folder == null )
            {
                throw new Exception( "FolderUnderSaveData [reflection] is null" );
            }

            // Call "FolderUnderSaveData" from null parameter, since this is a static method.
            return (string)Folder.Invoke( null, new object[] { "ManagerJobs" } );

        }

        private string DefaultSaveName()
        {
            // TODO: actually check saves and find a new one rather than rely on random gen.
            int i = 1;
            string name = _saveNameBase + i;
            while( SaveExists( name ) )
            {
                i++;
                name = _saveNameBase + i;
            }
            return name;
        }

        public override string Label
        {
            get
            {
                return "FM.ImportExport".Translate();
            }
        }

        public override ManagerJob Selected
        {
            // not used.
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public override void DoWindowContents( Rect canvas )
        {
            Rect loadRect = new Rect(_margin, _margin, (canvas.width - 3 * _margin) * _loadAreaRatio, canvas.height - 2 * _margin);
            Rect saveRect = new Rect(loadRect.xMax + _margin, _margin, canvas.width - 3 * _margin - loadRect.width, canvas.height - 2 * _margin);
            Widgets.DrawMenuSection( loadRect );
            Widgets.DrawMenuSection( saveRect );

            DrawLoadSection( loadRect );
            DrawSaveSection( saveRect );
        }

        private void DrawSaveSection( Rect rect )
        {
            Rect infoRect = new Rect(rect.ContractedBy(_margin));
            infoRect.height -= 30f + _margin;
            Rect nameRect = new Rect(rect.xMin + _margin, infoRect.yMax, (rect.width - 3 * _margin) / 2, 30f);
            Rect buttonRect = new Rect(nameRect.xMax + _margin, infoRect.yMax, nameRect.width, 30f );

            StringBuilder info = new StringBuilder();
            info.AppendLine( "FM.CurrentJobs".Translate() );
            foreach( Pair<string, int> jobCount in _jobCounts )
            {
                info.AppendLine( jobCount.First + ": " + jobCount.Second );
            }

            Widgets.Label( infoRect, info.ToString() );
            GUI.SetNextControlName( "ManagerJobsNameField" );
            string name = Widgets.TextField( nameRect, _saveName );
            if( GenText.IsValidFilename( name ) )
            {
                _saveName = name;
            }
            if( GenText.IsValidFilename( _saveName ) )
            {
                if( Widgets.TextButton( buttonRect, "FM.Export".Translate() ) )
                {
                    TryExport( _saveName );
                }
            }
            else
            {
                GUI.color = Color.gray;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.DrawBox( buttonRect );
                Widgets.Label( buttonRect, "FM.InvalidName".Translate() );
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
            }
        }

        private void DrawLoadSection( Rect rect )
        {
            if( _saveFiles.NullOrEmpty() )
            {
                // no saves found.
                GUI.color = Color.gray;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label( rect, "FM.NoSaves".Translate() );
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
            }
            else
            {
                GUI.BeginGroup( rect );
                Vector2 cur = Vector2.zero;
                try
                {
                    // TODO: Scrollable
                    int i = 1;
                    foreach( SaveFileInfo file in _saveFiles )
                    {
                        Rect row = new Rect(0f, cur.y, rect.width, _rowHeight);
                        if( i++ % 2 == 0 )
                        {
                            GUI.DrawTexture( row, Manager.OddRowBG );
                        }
                        DrawFileEntry( row, file );
                        cur.y += _rowHeight;
                    }
                } 
                finally
                {
                    // make sure it gets ended even if something fails.
                    GUI.EndGroup();
                }                
            }
        }

        private void DrawFileEntry( Rect rect, SaveFileInfo file )
        {
            GUI.BeginGroup( rect );

            // set up rects
            Rect nameRect = rect.AtZero();
            nameRect.width -= 200f + _iconSize + 4 * _margin;
            nameRect.xMin += _margin;
            Rect timeRect = new Rect(nameRect.xMax + _margin, 0f, 100f, rect.height );
            Rect buttonRect = new Rect(timeRect.xMax + _margin, 1f, 100f, rect.height - 2f );
            Rect deleteRect = new Rect(buttonRect.xMax + _margin, (rect.height - _iconSize) / 2, _iconSize, _iconSize);

            // name
            Widgets.Label( nameRect, Path.GetFileNameWithoutExtension( file.FileInfo.Name ) );

            // timestamp
            GUI.color = Color.gray;
            Dialog_MapList.DrawDateAndVersion( file, timeRect);
            Text.Font = GameFont.Small;
            GUI.color = Color.white;

            // load button
            if( Widgets.TextButton( buttonRect, "FM.Import".Translate() ) )
            {
                TryImport( file );
            }

            // delete button
            if( Widgets.ImageButton( deleteRect, Manager.DeleteX) )
            {
                Find.WindowStack.Add( new Dialog_Confirm( "ConfirmDelete".Translate( new object[]
                {
                        file.FileInfo.Name
                } ), delegate
                {
                    file.FileInfo.Delete();
                    Refresh();
                }, true ) );
            }


            GUI.EndGroup();
        }

        private bool SaveExists( string name )
        {
            return _saveFiles.Any( save => save.FileInfo.Name == name + _saveExtension );
        }

        private void TryImport( SaveFileInfo file )
        {
            // TODO some basic checks?
            DoImport( file );
        }

        private void DoImport( SaveFileInfo file )
        {
            try
            {
                // load stuff
                Scribe.InitLoading( _folder + "/" + file.FileInfo.Name );
                Manager.mode = Manager.Mode.importExport;
                ScribeHeaderUtility.LoadGameDataHeader( ScribeHeaderUtility.ScribeHeaderMode.Map );
                Scribe.EnterNode( "JobStack" );
                _jobStackIO.ExposeData();
                Scribe.ExitNode();
                Scribe.FinalizeLoading();

                // resolve crossreferences
                // these are registered during the loading stage, and cleared afterwards
                // will most definitely give errors/warnings on crossgame imports
                CrossRefResolver.ResolveAllCrossReferences();
                
                // replace the old jobstack
                Manager.Get.NewJobStack(_jobStackIO);

                // remove invalid jobs
                int invalid = 0;
                foreach( ManagerJob job in Manager.Get.JobStack.FullStack )
                {
                    if (!job.IsValid )
                    {
                        invalid++;
                        job.Delete(false);
                    }
                }

                // provide some feedback on failed import(s)
                // if debug is enabled the screen will also pop up with reference errors.
                if (invalid > 0 )
                {
                    Messages.Message( "FM.InvalidJobsDeleted".Translate( invalid ), MessageSound.SeriousAlert );
                }
            }
            catch( Exception e )
            {
                Log.Error( "Exception while loading jobstack: " + e.ToString() );
            }
            finally
            {
                // done?
                Scribe.mode = LoadSaveMode.Inactive;
                Manager.mode = Manager.Mode.normal;
                Messages.Message( "FM.JobsImported".Translate( _jobStackIO.FullStack.Count ), MessageSound.Standard );
                Refresh();
            }
        }

        private void TryExport( string name )
        {
            // if it exists, confirm overwrite
            if( SaveExists( name ) )
            {
                Find.WindowStack.Add( new Dialog_Confirm( "FM.ConfirmOverwrite".Translate( name ), delegate { DoExport( name ); }, true ) );
            } 
            else
            {
                DoExport( name );
            }
        }

        private void DoExport( string name )
        {
            try
            {
                try
                {
                    Scribe.InitWriting( FilePath( name ), "ManagerJobs" );
                }
                catch( Exception ex )
                {
                    GenUI.ErrorDialog( "ProblemSavingFile".Translate( new object[]
                    {
                        ex.ToString()
                    } ) );
                    throw;
                }
                ScribeHeaderUtility.WriteGameDataHeader();

                _jobStackIO = Manager.Get.JobStack;
                Scribe_Deep.LookDeep<JobStack>( ref _jobStackIO, "JobStack", new object[0] );
            }
            catch( Exception ex2 )
            {
                Log.Error( "Exception while saving jobstack: " + ex2.ToString() );
            }
            finally
            {
                Scribe.FinalizeWriting();
                Messages.Message( "FM.JobsExported".Translate( _jobStackIO.FullStack.Count ), MessageSound.Standard );
                Refresh();
            }
        }

        private string FilePath( string name )
        {
            return _folder + "/" + name + _saveExtension;
        }
    }
}
