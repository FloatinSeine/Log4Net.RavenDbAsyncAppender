using System;
using System.Linq;
using log4net.Core;
using Raven.Client;
using Raven.Client.Document;

namespace Log4Net.RavenDbAsyncAppender.Store
{
    public class LogEventStore : IDisposable
    {
        private bool _disposed;
        private IDocumentStore _documentStore;
        private IAsyncDocumentSession _documentSession;
        private object _lockObject;

        public int MaxNumberOfRequestsPerSession { get; set; }
        public IErrorHandler ErrorHandler { get; set; }

        public LogEventStore(string connectionString)
        {
            _lockObject = new object();
            Initialise(connectionString);
        }

        public void Store(LoggingEvent[] events)
        {
            if (events == null || !events.Any()) return;
            Array.ForEach(events, Store);
            Commit();
        }

        public void Store(LoggingEvent @event)
        {
            CheckSession();
            _documentSession.StoreAsync(@event);
        }

        protected virtual void Commit()
        {
            if (_documentSession == null) return;

            try
            {
                _documentSession.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                HandleError("Exception while commiting to the Raven DB", ex, ErrorCode.WriteFailure);
            }
        }

        private void Initialise(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString)) throw new Exception("Connection String is missing");
            if (_documentStore != null) return;

            try
            {
                _documentStore = new DocumentStore
                {
                    ConnectionStringName = connectionString
                };

                _documentStore.Initialize();
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to create Document Store. Connection String " + connectionString, ex);
            }
        }

        private void CheckSession()
        {
            lock (_lockObject)
            {
                if (_documentSession != null)
                {
                    if (HasMaxRequestSent)
                    {
                        Commit();
                        _documentSession.Dispose();
                    }
                    else
                    {
                        return;
                    }
                }
                CreateSession();
            }
        }

        private bool HasMaxRequestSent
        {
            get
            {
                return (_documentSession.Advanced.NumberOfRequests >=
                        _documentSession.Advanced.MaxNumberOfRequestsPerSession);
            }
        }

        private void CreateSession()
        {
            _documentSession = _documentStore.OpenAsyncSession();
            _documentSession.Advanced.UseOptimisticConcurrency = true;

            if (MaxNumberOfRequestsPerSession > 0)
            {
                _documentSession.Advanced.MaxNumberOfRequestsPerSession = MaxNumberOfRequestsPerSession;
            }
        }

        private void HandleError(string message, Exception ex, ErrorCode code)
        {
            ErrorHandler.Error(message, ex, code);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Commit();
                    _documentSession.Dispose();
                    _documentStore.Dispose();
                    _documentSession = null;
                    _documentStore = null;
                }
            }
            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
