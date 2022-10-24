// See https://aka.ms/new-console-template for more information
using NLog;

Logger logger = LogManager.GetCurrentClassLogger();
logger.Info("hello");
logger.Warn("hello");
logger.Error("hello");
