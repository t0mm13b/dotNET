using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Runtime.InteropServices;

// <%--<%@ Register TagPrefix="dbgAuxDS" Namespace="DebugAuxDS" %>--%>
// <dbgAuxDS:dbgDataSource ID="..." runat="server" CanTrace="False" DurationCache="5" CacheEnable="False" ConnectionString="..." />

namespace DebugAuxDS {
    /// <summary>
    /// Custom SqlDataSource for Debugging purposes, just find out what is the commands used!
    /// </summary>
    public class dbgDataSource : System.Web.UI.WebControls.SqlDataSource, IDisposable {
        private const string DBGCACHEKEY = "DebugAuxDSCacheKey";
        private const int DBGCACHESECS = 1;
        private bool _canTrace = false;
		private bool _cacheEnable = true;
		private int _cacheDuration = DBGCACHESECS;

        enum otDSCommand {
            Insert,
            Delete,
            Select,
            Update,
            DataBind
        };

        public dbgDataSource() {
            // Subscribe events!
            this.Inserting += dbgDataSource_Inserting;
            this.Deleting += dbgDataSource_Deleting;
            this.Selecting += dbgDataSource_Selecting;
            this.Updating += dbgDataSource_Updating;
            this.Selected += dbgDataSource_Selected;
            this.DataBinding += dbgDataSource_DataBinding;
            // Set the caching policy and duration
            this.EnableCaching = true;
            this.CacheKeyDependency = DBGCACHEKEY;
            this.CacheExpirationPolicy = System.Web.UI.DataSourceCacheExpiry.Absolute;
            this.CacheDuration = DBGCACHESECS; // Force it to be 1 sec!
        }


        public void Dispose() {
            // Set the caching policy and duration
            this.EnableCaching = false;
            // Unsubscribe events!
            this.DataBinding -= dbgDataSource_DataBinding;
            this.Selected -= dbgDataSource_Selected;
            this.Updating -= dbgDataSource_Updating;
            this.Selecting -= dbgDataSource_Selecting;
            this.Deleting -= dbgDataSource_Deleting;
            this.Inserting -= dbgDataSource_Inserting;
            // So long suckers!
            Dispose(true);
        }

        ~dbgDataSource() {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing) {
            if (disposing) {
                base.Dispose();
            }
        }
        public bool CanTrace {
            get {
                return _canTrace;
            }
            set {
                if (_canTrace != value) {
                    _canTrace = value;
                }
            }
        }
		
		public bool CacheEnable {
			get {
				return _cacheEnable;
			}
			set{
				if (_cacheEnable != value){
					_cacheEnable = value;
					this.EnableCaching = _cacheEnable;
					if (this.EnableCaching){
						this.CacheKeyDependency = DBGCACHEKEY;
						this.CacheExpirationPolicy = System.Web.UI.DataSourceCacheExpiry.Absolute;
						this.CacheDuration = _cacheDuration;
					}
				}
			}
		}
		
		public int DurationCache{
			get {
				return _cacheDuration;
			}
			set{
				if (_cacheDuration != value) {
					_cacheDuration = value;
					this.CacheDuration = _cacheDuration;
				}
			}
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dsCmdType"></param>
        /// <param name="dsCmd"></param>
        /// <param name="showStack"></param>
        private void dbgDataSourceLog(otDSCommand dsCmdType, String dsCmd, Boolean showStack) {
            bool b0rk3d = false;
            StringBuilder strStack = new StringBuilder();
            String headerMsg = String.Format("dbgDataSourceLog(...) - dsCmdType: {0};  dsCmd: {1}", dsCmdType.ToString(), dsCmd);
            try {
                if (showStack) {
                    System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace();
                    System.Diagnostics.StackFrame stCurrFrame = st.GetFrame(1);
                    foreach (System.Diagnostics.StackFrame currSF in st.GetFrames()) {
                        if (!(stCurrFrame.Equals(currSF))) {
                            if (!(currSF.GetMethod().ToString().ToLower().Equals("dbgDataSourceLog"))) {
                                System.Reflection.MethodBase mb = currSF.GetMethod();
                                strStack.AppendFormat("{0}.{1}:{2}", mb.DeclaringType.Namespace, mb.DeclaringType.Name, mb.Name);
                                strStack.Append(System.Environment.NewLine);
                            }
                        }
                    }

                }
            } catch (Exception eX) {
                b0rk3d = true;
            }
            if (!showStack) {
                System.Diagnostics.Debug.WriteLine(headerMsg);
            } else {
                if (b0rk3d) {
                    System.Diagnostics.Debug.WriteLine(headerMsg);
                } else {
                    strStack.Insert(0, String.Format("{0}{1}", headerMsg, System.Environment.NewLine));
                    System.Diagnostics.Debug.WriteLine(strStack.ToString());
                }
            }
        }

        private void InvalidateCache() {
            try {
                if (this.Page != null) {
                    if (this.Page.Cache != null) {
                        if (this.Page.Cache[DBGCACHEKEY] == null) {
                            this.Page.Cache[DBGCACHEKEY] = DateTime.Now.Ticks;
                        } else {
                            this.Page.Cache[DBGCACHEKEY] = DateTime.Now.Ticks; // Force the invalidate of SqlDataSource
                        }
                    } else {
                        // Forget it.... must not interfere with page from here!
                    }
                } else {
                    // Hmmm... this should not happen...
                }
            }catch(NullReferenceException nullRefEx) {

            } catch (InvalidOperationException invOpEx) {

            } catch (Exception eX) {

            }
        }
        void dbgDataSource_Selected(object sender, System.Web.UI.WebControls.SqlDataSourceStatusEventArgs e) {
            // After the select - we clear it to ensure it picks up latest and greatest... dunno
            this.InvalidateCache();
        }

        void dbgDataSource_Updating(object sender, System.Web.UI.WebControls.SqlDataSourceCommandEventArgs e) {
            if (_canTrace) this.dbgDataSourceLog(otDSCommand.Update, e.Command.CommandText, true);
        }

        void dbgDataSource_Selecting(object sender, System.Web.UI.WebControls.SqlDataSourceSelectingEventArgs e) {
            if (_canTrace) this.dbgDataSourceLog(otDSCommand.Select, e.Command.CommandText, true);
        }

        void dbgDataSource_Inserting(object sender, System.Web.UI.WebControls.SqlDataSourceCommandEventArgs e) {
            if (_canTrace) this.dbgDataSourceLog(otDSCommand.Insert, e.Command.CommandText, true);
        }

        void dbgDataSource_Deleting(object sender, System.Web.UI.WebControls.SqlDataSourceCommandEventArgs e) {
            if (_canTrace) this.dbgDataSourceLog(otDSCommand.Delete, e.Command.CommandText, true);
        }

        // Should we invalidate the cache here for databinding .... ?
        void dbgDataSource_DataBinding(object sender, EventArgs e) {
            if (_canTrace) this.dbgDataSourceLog(otDSCommand.DataBind, "DataBinding", true);
            this.InvalidateCache();
        }
        [DllImport("kernel32.dll")]
        static extern void OutputDebugString(string lpOutputString);

        
    }
}
