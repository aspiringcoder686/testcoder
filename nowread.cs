using System;
using System.Collections.Generic;

public class RootConfig
{
    public ConnectionStringsConfig ConnectionStrings { get; set; }
    public SpringSettingsConfig SpringSettings { get; set; }
    public HibernateSessionFactoryConfig HibernateSessionFactory { get; set; }
    public TransactionManagerConfig TransactionManager { get; set; }
    public EntityManagerConfig EntityManager { get; set; }
    public MongoEntityManagerFactoryConfig MongoEntityManagerFactory { get; set; }
    public MongoEntityManagerConfig MongoEntityManager { get; set; }
    public FullAccessAssetInfoManagerConfig FullAccessAssetInfoManager { get; set; }
    public FullAccessAdminManagerConfig FullAccessAdminManager { get; set; }
    public ConfigManagerConfig ConfigManager { get; set; }
    public FileHibernateSessionFactoryConfig FileHibernateSessionFactory { get; set; }
    public FileTransactionManagerConfig FileTransactionManager { get; set; }
    public ModelUploadServiceClientConfig ModelUploadServiceClient { get; set; }
    public TemplateManagerConfig TemplateManager { get; set; }
    public ConversionManagerConfig ConversionManager { get; set; }
    public NotificationEmailManagerConfig NotificationEmailManager { get; set; }
    public AutomationLoadBalancerConfig AutomationLoadBalancer { get; set; }
    public VelocityTemplateManagerConfig VelocityTemplateManager { get; set; }
    public MailManagerConfig MailManager { get; set; }
    public ModelUploadManagerConfig ModelUploadManager { get; set; }
    public LegacySmttoXmlAdapterConfig LegacySmttoXmlAdapter { get; set; }
    public SmtSummaryTransformerConfig SmtSummaryTransformer { get; set; }
    public SmtTimeSeriesTransformerConfig SmtTimeSeriesTransformer { get; set; }
    public BulkUploadManagerConfig BulkUploadManager { get; set; }
    public EmailNotificationHelperConfig EmailNotificationHelper { get; set; }
    public AppRoutingManagerConfig AppRoutingManager { get; set; }
    public SmtSingleUploadManagerConfig SmtSingleUploadManager { get; set; }
    public ValuationSingleUploadManagerConfig ValuationSingleUploadManager { get; set; }
    public UnsecuredValuationSingleUploadManagerConfig UnsecuredValuationSingleUploadManager { get; set; }
    public ReportingInfoAmsManagerConfig ReportingInfoAmsManager { get; set; }
    public ValuationUploadManagerConfig ValuationUploadManager { get; set; }
    public ValuationsRefreshManagerConfig ValuationsRefreshManager { get; set; }
    public DataFileManagerConfig DataFileManager { get; set; }
    public FundLevelExpensesManagerConfig FundLevelExpensesManager { get; set; }
    public EquitySummaryManagerConfig EquitySummaryManager { get; set; }
    public CashFlowVarianceReportManagerConfig CashFlowVarianceReportManager { get; set; }
    public AddinVersionManagerConfig AddinVersionManager { get; set; }
    public LiborManagerConfig LiborManager { get; set; }
    public AssetReservesManagerConfig AssetReservesManager { get; set; }
    public BulkBluebooksManagerConfig BulkBluebooksManager { get; set; }
    public ExitProbabilityManagerConfig ExitProbabilityManager { get; set; }
    public LedgerManagerConfig LedgerManager { get; set; }
    public ValuationAttributesManagerConfig ValuationAttributesManager { get; set; }
    public ApplicationStatusManagerConfig ApplicationStatusManager { get; set; }
    public IrrBridgeManagerConfig IrrBridgeManager { get; set; }
    public TacticalPlanManagerConfig TacticalPlanManager { get; set; }
    public DebtManagerConfig DebtManager { get; set; }
    public TeamMemberManagerConfig TeamMemberManager { get; set; }
    public AssetInfoManagerConfig AssetInfoManager { get; set; }
    public UploadDisplayManagerConfig UploadDisplayManager { get; set; }
    public AdminManagerConfig AdminManager { get; set; }
    public FieldDictionaryManagerConfig FieldDictionaryManager { get; set; }
    public ApplicationManagerConfig ApplicationManager { get; set; }
    public ModelManagerConfig ModelManager { get; set; }
    public ReportingManagerConfig ReportingManager { get; set; }
    public ValuationsWorkflowManagerConfig ValuationsWorkflowManager { get; set; }
    public ValuationsSecondLevelApprovalManagerConfig ValuationsSecondLevelApprovalManager { get; set; }
    public ValuationApprovalEmailValidatorConfig ValuationApprovalEmailValidator { get; set; }
    public ValuationApprovalEmailProcessorConfig ValuationApprovalEmailProcessor { get; set; }
    public ValuationSecondLevelApprovalEmailProcessorConfig ValuationSecondLevelApprovalEmailProcessor { get; set; }
    public EmailCollectorConfig EmailCollector { get; set; }
    public DeferredSmtpServiceConfig DeferredSmtpService { get; set; }
    public DownloadProcessingJobConfig DownloadProcessingJob { get; set; }
    public DownloadProcessingJobTriggerConfig DownloadProcessingJobTrigger { get; set; }
    public BatchProcessingJobConfig BatchProcessingJob { get; set; }
    public BatchProcessingJobTriggerConfig BatchProcessingJobTrigger { get; set; }
    public ConversionProcessingJobConfig ConversionProcessingJob { get; set; }
    public TaskNotificationWatcherJobConfig TaskNotificationWatcherJob { get; set; }
    public TaskNotificationWatcherJobTriggerConfig TaskNotificationWatcherJobTrigger { get; set; }
    public TaskDueDateNotificationWatcherJobConfig TaskDueDateNotificationWatcherJob { get; set; }
    public TaskDueDateNotificationWatcherJobTriggerConfig TaskDueDateNotificationWatcherJobTrigger { get; set; }
    public HeartBeatNotificationJobConfig HeartBeatNotificationJob { get; set; }
    public HeartBeatNotificationJobTriggerConfig HeartBeatNotificationJobTrigger { get; set; }
    public SecurityCacheProcessingJobConfig SecurityCacheProcessingJob { get; set; }
    public SecurityCacheProcessingJobTriggerConfig SecurityCacheProcessingJobTrigger { get; set; }
    public ActiveDirectoryCacheProcessingJobConfig ActiveDirectoryCacheProcessingJob { get; set; }
    public ActiveDirectoryCacheProcessingJobAppStartTriggerConfig ActiveDirectoryCacheProcessingJobAppStartTrigger { get; set; }
    public ActiveDirectoryCacheProcessingJobDailyRefreshTriggerConfig ActiveDirectoryCacheProcessingJobDailyRefreshTrigger { get; set; }
    public DeferredSmtpServiceJobConfig DeferredSmtpServiceJob { get; set; }
    public DeferredSmtpServiceTriggerConfig DeferredSmtpServiceTrigger { get; set; }
    public UserProvisioningJobConfig UserProvisioningJob { get; set; }
    public UserProvisioningJobTriggerConfig UserProvisioningJobTrigger { get; set; }
    public QuartzSchedulerFactoryConfig QuartzSchedulerFactory { get; set; }
    public EventNotificationMangerConfig EventNotificationManger { get; set; }
    public TaskManagerConfig TaskManager { get; set; }
    public TaskScheduleManagerConfig TaskScheduleManager { get; set; }
    public ValuationReconciliationManagerConfig ValuationReconciliationManager { get; set; }
    public TaskTypeManagerConfig TaskTypeManager { get; set; }
    public HeartBeatManagerConfig HeartBeatManager { get; set; }
}

public class ConnectionStringsConfig
{
    public string DbProvider { get; set; }
    public string FileDbProvider { get; set; }
}

public class SpringSettingsConfig
{
    public string ConnectionString { get; set; }
    public bool UseHangfireMemoryStorage { get; set; }
}

public class HibernateSessionFactoryConfig
{
    public List<string> MappingAssemblies { get; set; }
    public HibernatePropertiesConfig HibernateProperties { get; set; }
    public List<object> Interceptors { get; set; }
}

public class HibernatePropertiesConfig
{
    public string DefaultSchema { get; set; }
    public bool ShowSql { get; set; }
    public bool FormatSql { get; set; }
    public bool UseSqlComments { get; set; }
    public string QuerySubstitutions { get; set; }
    public bool CacheUseSecondLevelCache { get; set; }
    public bool CacheUseQueryCache { get; set; }
    public string CacheProviderClass { get; set; }
    public string ConnectionDriverClass { get; set; }
}

public class TransactionManagerConfig
{
    public int DefaultTimeout { get; set; }
}

public class EntityManagerConfig
{
    public string ConnectionString { get; set; }
    public string ReportingDbConnectionString { get; set; }
    public string NotificationListForSpSaveError { get; set; }
}

public class MongoEntityManagerFactoryConfig
{
    public string ServerName { get; set; }
}

public class MongoEntityManagerConfig
{
    public string DatabaseName { get; set; }
    public string MongoEmailRecipients { get; set; }
}

public class FullAccessAssetInfoManagerConfig
{
    public string AssetImageUrl { get; set; }
}

public class FullAccessAdminManagerConfig
{
    public string CurrentReportingPeriodConfigCode { get; set; }
    public string DefaultCurrentReportingPeriodConfigXml { get; set; }
    public string CurrentReportingPeriodSMTConfigCode { get; set; }
    public string DefaultCurrentReportingPeriodSMTConfigXml { get; set; }
}

public class ConfigManagerConfig
{
    public string _key { get; set; }
    public Dictionary<string, object> _inMemoryCache { get; set; }
}

public class FileHibernateSessionFactoryConfig
{
    public List<string> MappingAssemblies { get; set; }
    public HibernatePropertiesConfig HibernateProperties { get; set; }
    public List<object> Interceptors { get; set; }
}

public class FileTransactionManagerConfig
{
    public int DefaultTimeout { get; set; }
}

public class ModelUploadServiceClientConfig
{
    public string AppEnvironment { get; set; }
    public Dictionary<string, string> ReportingServerEndpoints { get; set; }
    public Dictionary<string, string> ModelUploadServiceEndpointUris { get; set; }
}

public class TemplateManagerConfig
{
    public string BlankTemplateDirectory { get; set; }
    public string TaskExceptionsTemplate { get; set; }
    public Dictionary<string, string> BlankTemplateFiles { get; set; }
    public Dictionary<string, string> WorkbookIdentificationAndVersionFiles { get; set; }
    public Dictionary<string, string> GenericWorkbookConversionFiles { get; set; }
    public Dictionary<string, string> MappingFiles { get; set; }
    public Dictionary<string, string> ChangeSetFiles { get; set; }
    public Dictionary<string, string> TemplateControlFiles { get; set; }
}

public class ConversionManagerConfig
{
    public string DefaultNotificationRecipient { get; set; }
    public string ResponseServiceUri { get; set; }
    public int ConversionProcessingTimeoutMinutes { get; set; }
    public string SMTUploadsDate { get; set; }
    public string SMTPreviousVersion { get; set; }
    public string VALUploadsDate { get; set; }
    public string VALPreviousVersion { get; set; }
    public string OutputConversionFolder { get; set; }
    public string OutputErrorConversionFolder { get; set; }
    public string OutputOriginalFolder { get; set; }
    public string OutputUploadsCsvFolder { get; set; }
    public string ModelConversionCallbackUrl { get; set; }
}

public class NotificationEmailManagerConfig
{
    public string MDMMissingNotificationRecipients { get; set; }
}

public class AutomationLoadBalancerConfig
{
    public int MaxAsynchronousRequestsPerServer { get; set; }
    public int MaxRealtimeRequestsPerServer { get; set; }
    public string RealtimeServerAddresses { get; set; }
    public string AsynchronousServerAddresses { get; set; }
    public int TokenTimeLimit { get; set; }
}

public class VelocityTemplateManagerConfig
{
    public Dictionary<string, string> VelocityEngine { get; set; }
    public string DefaultTemplatesLocation { get; set; }
}

public class MailManagerConfig
{
    public string MailServer { get; set; }
    public string ApplicationEnvironment { get; set; }
    public string Application { get; set; }
    public string From { get; set; }
    public string ApplicationStateNotification { get; set; }
    public string ApplicationStateNotificationRecipients { get; set; }
}

public class ModelUploadManagerConfig
{
    public string SmtBOXsltFile { get; set; }
    public string DefaultStatusCode { get; set; }
    public string RiskSeverityCode { get; set; }
}

public class LegacySmttoXmlAdapterConfig
{
    public string Namespace { get; set; }
}

public class SmtSummaryTransformerConfig
{
    public string SheetName { get; set; }
}

public class SmtTimeSeriesTransformerConfig
{
    public string SheetName { get; set; }
    public string DatesStartRange { get; set; }
}

public class BulkUploadManagerConfig
{
    public bool SendBatchStatusNotifications { get; set; }
    public string BlankTemplatePath { get; set; }
    public string BlankTemplateFileName { get; set; }
    public BusinessRulesTransformerConfig BusinessRulesTransformer { get; set; }
    public Dictionary<string, object> Sheets { get; set; }
    public string TogglerDebtHeadersValidationCriteria { get; set; }
    public string TogglerDebtTargetValueColumn { get; set; }
    public string TimeSeriesSheetName { get; set; }
    public string SummarySheetName { get; set; }
    public Dictionary<string, string> ValuationsCalculationSheetNames { get; set; }
    public string ValuationsTypeCell { get; set; }
    public string ValuationsTogglerWorksheet { get; set; }
    public string ValuationsTogglerName { get; set; }
    public string ValuationsTogglerListOfValuesStartLocation { get; set; }
    public string BlankValuationsTemplatePath { get; set; }
    public string BlankValuationsTemplateFileName { get; set; }
    public string BlankBulkAddressTemplatePath { get; set; }
    public string BlankBulkAddressTemplateFileName { get; set; }
    public string BlankFMVTemplatePath { get; set; }
    public string BlankFMVTemplateFileName { get; set; }
}

public class BusinessRulesTransformerConfig
{
    public string Worksheet { get; set; }
}

public class EmailNotificationHelperConfig
{
    public string BccEmail { get; set; }
    public string ConversionSuccessTemplate { get; set; }
    public string ConversionSuccessSimplifiedTemplate { get; set; }
    public string ConversionFailureTemplate { get; set; }
    public string DownloadSuccessTemplate { get; set; }
    public string DownloadFailureTemplate { get; set; }
    public string ApplicationStateNotification { get; set; }
    public string ApplicationStateNotificationRecipients { get; set; }
    public string ProductionSupportEmailAddress { get; set; }
    public string InvestmentSupportEmailAddress { get; set; }
}

public class AppRoutingManagerConfig
{
    public string RouteDefinitionFile { get; set; }
    public string ApplicationEnvironment { get; set; }
}

public class SmtSingleUploadManagerConfig
{
    public string TimeSeriesSheetName { get; set; }
    public string SummarySheetName { get; set; }
    public string DatesStartRange { get; set; }
    public string MonthlyCustomFieldStartRange { get; set; }
    public Dictionary<string, object> Sheets { get; set; }
    public string LiborRow { get; set; }
    public string SOFRRow { get; set; }
    public string PRIMERow { get; set; }
    public string TenYrRow { get; set; }
    public Dictionary<string, string> ATSMRows { get; set; }
    public Dictionary<string, string> CashFlowsRevenueAndExpenseRows { get; set; }
    public Dictionary<string, object> HiddenRows { get; set; }
    public Dictionary<string, object> UnHiddenRows { get; set; }
    public Dictionary<string, object> AllowedUserRoles { get; set; }
    public Dictionary<string, object> UserCreationHelper { get; set; }
    public string LoanEmailNotificationType { get; set; }
    public string WorkbookChecksumCell { get; set; }
    public string AsOfDateCell { get; set; }
}

public class ValuationSingleUploadManagerConfig
{
    public string TestFileUploadPath { get; set; }
    public string TestFileOutputPath { get; set; }
    public string WorkbookChecksumCell { get; set; }
    public string MostRecentSMT { get; set; }
    public string BRFormula { get; set; }
    public string FmvThreshold { get; set; }
    public string BRErrorMessage { get; set; }
    public string BRSeverity { get; set; }
    public string AssetFMVCell { get; set; }
    public string BRCode { get; set; }
    public BusinessRulesTransformerConfig BusinessRulesTransformer { get; set; }
    public Dictionary<string, string> LastColInfo { get; set; }
    public Dictionary<string, object> HiddenValuationRanges { get; set; }
    public Dictionary<string, object> AllowedUserRoles { get; set; }
    public Dictionary<string, string> AssetTypeRanges { get; set; }
    public Dictionary<string, object> CalculationSheetNames { get; set; }
    public Dictionary<string, object> UserCreationHelper { get; set; }
    public string ValuationsTypeCell { get; set; }
}

public class UnsecuredValuationSingleUploadManagerConfig : ValuationSingleUploadManagerConfig
{}

public class ReportingInfoAmsManagerConfig
{
    public string NotificationListForSaveError { get; set; }
    public bool SaveUploadReportingInfoInAMS { get; set; }
}

public class ValuationUploadManagerConfig
{
    public string XMLNamespace { get; set; }
    public string XMLURI { get; set; }
    public string XMLSchemaLocation { get; set; }
    public string XSLTFileOne { get; set; }
    public string XSLTFileTwo { get; set; }
    public string XSDSchemaFile { get; set; }
    public string FmvThreshold { get; set; }
}

public class ValuationsRefreshManagerConfig
{
    public string DriverMetricCell { get; set; }
}

public class DataFileManagerConfig
{
    public TransformerConfig Transformer { get; set; }
}

public class TransformerConfig
{
    public string MappingSource { get; set; }
}

public class FundLevelExpensesManagerConfig
{
    public bool SaveUploadReportingInfoInAMS { get; set; }
    public string NotificationListForFLESaveError { get; set; }
    public string SheetName { get; set; }
    public string DatesStartRange { get; set; }
    public string AsOfDateCell { get; set; }
    public string TemplateVersion { get; set; }
    public string InitialDataRow { get; set; }
    public int NumberOfRowsInFundSection { get; set; }
    public Dictionary<string, object> HiddenFunds { get; set; }
    public Dictionary<string, object> AllowedUserRoles { get; set; }
    public Dictionary<string, object> Sheets { get; set; }
    public Dictionary<string, object> UserCreationHelper { get; set; }
    public string WorkbookChecksumCell { get; set; }
}

public class EquitySummaryManagerConfig
{
    public string ReportingPeriodRow { get; set; }
    public string ReportingPeriodCol { get; set; }
    public bool SaveUploadReportingInfoInAMS { get; set; }
    public string NotificationListForESSaveError { get; set; }
    public Dictionary<string, object> AllowedUserRoles { get; set; }
    public Dictionary<string, object> Sheets { get; set; }
    public Dictionary<string, object> UserCreationHelper { get; set; }
    public EquitySummaryXmlTransformerConfig EquitySummaryXmlTransformer { get; set; }
}

public class EquitySummaryXmlTransformerConfig
{
}

public class CashFlowVarianceReportManagerConfig
{
    public string XmlHierarchyFileLocation { get; set; }
}

public class AddinVersionManagerConfig
{
    public string LatestAddinVersion { get; set; }
}

public class LiborManagerConfig
{
    public string ReportingPeriodRow { get; set; }
    public string ReportingPeriodCol { get; set; }
    public string StartDataCell { get; set; }
    public string WorksheetName { get; set; }
    public string UploadTypeCode { get; set; }
    public Dictionary<string, object> AllowedUserRoles { get; set; }
}

public class AssetReservesManagerConfig
{
    public string WorksheetName { get; set; }
    public string AsOfDateCell { get; set; }
    public string AssetNameCell { get; set; }
    public string StartDataCell { get; set; }
    public string HeaderCase3J21 { get; set; }
    public string HeaderCase1J31 { get; set; }
    public string HeaderCase2J41 { get; set; }
    public int NumberOfColumns { get; set; }
    public string UploadTypeCode { get; set; }
    public Dictionary<string, object> AllowedUserRoles { get; set; }
    public Dictionary<string, string> LastColInfo { get; set; }
}

public class BulkBluebooksManagerConfig
{
    public bool SaveUploadReportingInfoInAMS { get; set; }
    public string NotificationListForBulkBBSaveError { get; set; }
    public string SheetName { get; set; }
    public string ValuesStartRange { get; set; }
    public string AsOfDateCell { get; set; }
    public string TemplateVersion { get; set; }
    public int InitialDataRow { get; set; }
    public int NumberOfRowsInFundSection { get; set; }
    public Dictionary<string, object> AllowedUserRoles { get; set; }
}

public class ExitProbabilityManagerConfig
{
    public Dictionary<string, object> AllowedUserRoles { get; set; }
    public Dictionary<string, object> Sheets { get; set; }
}

public class LedgerManagerConfig
{
    public int MaxBlankLinesBeforeStopping { get; set; }
    public string TestFileUploadPath { get; set; }
    public string TestFileOutputPath { get; set; }
    public string BlankLedgerFilePath { get; set; }
    public string BlankLedgerFileName { get; set; }
}

public class ValuationAttributesManagerConfig
{
    public int MaxBlankLinesBeforeStopping { get; set; }
    public string TestFileUploadPath { get; set; }
    public string TestFileOutputPath { get; set; }
    public string BlankValuationAttributesFilePath { get; set; }
    public string BlankLedgerFileName { get; set; }
}

public class ApplicationStatusManagerConfig
{
    public string ApplicationStatusCode { get; set; }
    public string OnStatusXML { get; set; }
}

public class IrrBridgeManagerConfig
{
    public int CommentsRequiredThreshold { get; set; }
    public int CapCommentsRequiredThreshold { get; set; }
    public SmtTransformerConfig SmtTransformer { get; set; }
    public IRRBridgeDataTransformerConfig IRRBridgeDataTransformer { get; set; }
    public ReportSectionProcessorConfig ReportSectionProcessor { get; set; }
}

public class SmtTransformerConfig { }
public class IRRBridgeDataTransformerConfig { }
public class ReportSectionProcessorConfig { }

public class TacticalPlanManagerConfig
{
    public TacticalPlanAdapterConfig TacticalPlanAdapter { get; set; }
}

public class TacticalPlanAdapterConfig { }

public class DebtManagerConfig
{
    public string DefaultStatusCode { get; set; }
    public string RiskSeverityCode { get; set; }
    public string DefaultLoanBusinessId { get; set; }
}

public class TeamMemberManagerConfig
{
    public string IncludeTestUsers { get; set; }
    public string ExternalUserGroup { get; set; }
}

public class AssetInfoManagerConfig
{
    public string AssetImageUrl { get; set; }
}

public class UploadDisplayManagerConfig
{
    public string MaxFileSizeCanBeDownloadedSynch { get; set; }
    public string DownloadLinkExpirationInHours { get; set; }
    public string DownloadProcessingTimeoutMinutes { get; set; }
    public string AMSUserDownloadFolder { get; set; }
}

public class AdminManagerConfig : FullAccessAdminManagerConfig { }

public class FieldDictionaryManagerConfig
{
    public int FieldDictionaryCacheSeconds { get; set; }
    public string LoanIdItemBlock { get; set; }
    public Dictionary<string, string> FieldGuids { get; set; }
}

public class ApplicationManagerConfig
{
    public string AMSUrl { get; set; }
    public string AmsEnvironment { get; set; }
    public string AmsTitle { get; set; }
    public string MaxFileSize { get; set; }
}

public class ModelManagerConfig
{
    public string AdditionalExtensionsPath { get; set; }
}

public class ReportingManagerConfig
{
    public string AdditionalExtensionsPath { get; set; }
    public DefaultReportPreferencesConfig DefaultReportPreferences { get; set; }
    public Dictionary<string, string> CannedReports { get; set; }
    public string SystemEmailAccount { get; set; }
}

public class DefaultReportPreferencesConfig
{
    public string ValuationDataExtract { get; set; }
    public string SmtDataExtract { get; set; }
}

public class ValuationsWorkflowManagerConfig
{
    public Dictionary<string, object> AllowedUserRoles { get; set; }
    public Dictionary<string, object> AllowedUsers { get; set; }
    public string ReviewEmailTemplateFile { get; set; }
    public string ReceiptEmailTemplateFile { get; set; }
    public string ReviewEmailRowFile { get; set; }
    public string ReviewEmailRowWithCommentsFile { get; set; }
    public string InvalidUploadsTemplateFile { get; set; }
    public string MissingUploasTemplateFile { get; set; }
    public string InvalidApproverEmailTemplateFile { get; set; }
    public string UploadLink { get; set; }
    public int UserInfoLookupCacheSeconds { get; set; }
    public string SystemEmailAccount { get; set; }
    public string DueDateText { get; set; }
    public string SubcommitteeText { get; set; }
}

public class ValuationsSecondLevelApprovalManagerConfig : ValuationsWorkflowManagerConfig { }

public class ValuationApprovalEmailValidatorConfig
{
    public string LevenshteinThreshold { get; set; }
    public Dictionary<string, string> ApproveWhiteList { get; set; }
    public Dictionary<string, string> RejectWhiteList { get; set; }
}

public class ValuationApprovalEmailProcessorConfig
{
    public string ReceiptAddress { get; set; }
}

public class ValuationSecondLevelApprovalEmailProcessorConfig : ValuationApprovalEmailProcessorConfig { }

public class EmailCollectorConfig
{
    public string Account { get; set; }
    public string ExchangeClientId { get; set; }
    public string ExchangeClientSecret { get; set; }
    public string ExchangeTenantId { get; set; }
    public string SyncStateFilePath { get; set; }
    public bool EnableEmailCollection { get; set; }
    public int PageSize { get; set; }
    public List<object> EmailProcessors { get; set; }
}

public class DeferredSmtpServiceConfig
{
    public string DefaultNotificationRecipient { get; set; }
}

public class DownloadProcessingJobConfig
{
    public string TargetMethod { get; set; }
    public string Concurrent { get; set; }
}

public class DownloadProcessingJobTriggerConfig
{
    public string StartDelay { get; set; }
    public string RepeatInterval { get; set; }
}

public class BatchProcessingJobConfig : DownloadProcessingJobConfig { }
public class BatchProcessingJobTriggerConfig : DownloadProcessingJobTriggerConfig { }
public class ConversionProcessingJobConfig : DownloadProcessingJobConfig { }
public class TaskNotificationWatcherJobConfig : DownloadProcessingJobConfig { }
public class TaskNotificationWatcherJobTriggerConfig : DownloadProcessingJobTriggerConfig { }
public class TaskDueDateNotificationWatcherJobConfig : DownloadProcessingJobConfig { }
public class TaskDueDateNotificationWatcherJobTriggerConfig : DownloadProcessingJobTriggerConfig { }
public class HeartBeatNotificationJobConfig : DownloadProcessingJobConfig { }
public class HeartBeatNotificationJobTriggerConfig : DownloadProcessingJobTriggerConfig { }

public class SecurityCacheProcessingJobConfig : DownloadProcessingJobConfig { }
public class SecurityCacheProcessingJobTriggerConfig : DownloadProcessingJobTriggerConfig { }
public class ActiveDirectoryCacheProcessingJobConfig
{
    public string TargetMethod { get; set; }
    public Dictionary<string, string> NamedArguments { get; set; }
}
public class ActiveDirectoryCacheProcessingJobAppStartTriggerConfig : DownloadProcessingJobTriggerConfig { }
public class ActiveDirectoryCacheProcessingJobDailyRefreshTriggerConfig
{
    public string CronExpressionString { get; set; }
}

public class DeferredSmtpServiceJobConfig : DownloadProcessingJobConfig { }
public class DeferredSmtpServiceTriggerConfig
{
    public string CronExpressionString { get; set; }
}
public class UserProvisioningJobConfig : DownloadProcessingJobConfig { }
public class UserProvisioningJobTriggerConfig
{
    public string CronExpressionString { get; set; }
}

public class QuartzSchedulerFactoryConfig
{
    public bool AutoStartup { get; set; }
    public List<object> Triggers { get; set; }
}

public class EventNotificationMangerConfig
{
    public Dictionary<string, object> Observers { get; set; }
}

public class TaskManagerConfig
{
    public string TaskServiceUrl { get; set; }
}

public class TaskScheduleManagerConfig
{
    public string WorksheetName { get; set; }
}

public class ValuationReconciliationManagerConfig
{
    public string NotificationCc { get; set; }
}

public class TaskTypeManagerConfig : TaskManagerConfig { }

public class HeartBeatManagerConfig
{
    public int FailuresQuantity { get; set; }
    public Dictionary<string, string> EmailReceipentsForMongoHeartbeatError { get; set; }
}

 (or Program) via the following: you can now auto-wire all of them in your Startup (or Program) via the following:

// *******************************
// Extension method to bind all configuration sections
// *******************************
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class ConfigurationExtensions
{
    public static IServiceCollection AddAppConfiguration(this IServiceCollection services, IConfiguration config)
    {
        // Bind each section to its corresponding Config class
        services.Configure<ConnectionStringsConfig>(config.GetSection("ConnectionStrings"));
        services.Configure<SpringSettingsConfig>(config.GetSection("SpringSettings"));
        services.Configure<HibernateSessionFactoryConfig>(config.GetSection("HibernateSessionFactory"));
        services.Configure<TransactionManagerConfig>(config.GetSection("TransactionManager"));
        services.Configure<EntityManagerConfig>(config.GetSection("EntityManager"));
        services.Configure<MongoEntityManagerFactoryConfig>(config.GetSection("MongoEntityManagerFactory"));
        services.Configure<MongoEntityManagerConfig>(config.GetSection("MongoEntityManager"));
        services.Configure<FullAccessAssetInfoManagerConfig>(config.GetSection("FullAccessAssetInfoManager"));
        services.Configure<FullAccessAdminManagerConfig>(config.GetSection("FullAccessAdminManager"));
        services.Configure<ConfigManagerConfig>(config.GetSection("ConfigManager"));
        services.Configure<FileHibernateSessionFactoryConfig>(config.GetSection("FileHibernateSessionFactory"));
        services.Configure<FileTransactionManagerConfig>(config.GetSection("FileTransactionManager"));
        services.Configure<ModelUploadServiceClientConfig>(config.GetSection("ModelUploadServiceClient"));
        services.Configure<TemplateManagerConfig>(config.GetSection("TemplateManager"));
        services.Configure<ConversionManagerConfig>(config.GetSection("ConversionManager"));
        services.Configure<NotificationEmailManagerConfig>(config.GetSection("NotificationEmailManager"));
        services.Configure<AutomationLoadBalancerConfig>(config.GetSection("AutomationLoadBalancer"));
        services.Configure<VelocityTemplateManagerConfig>(config.GetSection("VelocityTemplateManager"));
        services.Configure<MailManagerConfig>(config.GetSection("MailManager"));
        services.Configure<ModelUploadManagerConfig>(config.GetSection("ModelUploadManager"));
        services.Configure<LegacySmttoXmlAdapterConfig>(config.GetSection("LegacySmttoXmlAdapter"));
        services.Configure<SmtSummaryTransformerConfig>(config.GetSection("SmtSummaryTransformer"));
        services.Configure<SmtTimeSeriesTransformerConfig>(config.GetSection("SmtTimeSeriesTransformer"));
        services.Configure<BulkUploadManagerConfig>(config.GetSection("BulkUploadManager"));
        services.Configure<EmailNotificationHelperConfig>(config.GetSection("EmailNotificationHelper"));
        services.Configure<AppRoutingManagerConfig>(config.GetSection("AppRoutingManager"));
        services.Configure<SmtSingleUploadManagerConfig>(config.GetSection("SmtSingleUploadManager"));
        services.Configure<ValuationSingleUploadManagerConfig>(config.GetSection("ValuationSingleUploadManager"));
        services.Configure<UnsecuredValuationSingleUploadManagerConfig>(config.GetSection("UnsecuredValuationSingleUploadManager"));
        services.Configure<ReportingInfoAmsManagerConfig>(config.GetSection("ReportingInfoAmsManager"));
        services.Configure<ValuationUploadManagerConfig>(config.GetSection("ValuationUploadManager"));
        services.Configure<ValuationsRefreshManagerConfig>(config.GetSection("ValuationsRefreshManager"));
        services.Configure<DataFileManagerConfig>(config.GetSection("DataFileManager"));
        services.Configure<FundLevelExpensesManagerConfig>(config.GetSection("FundLevelExpensesManager"));
        services.Configure<EquitySummaryManagerConfig>(config.GetSection("EquitySummaryManager"));
        services.Configure<CashFlowVarianceReportManagerConfig>(config.GetSection("CashFlowVarianceReportManager"));
        services.Configure<AddinVersionManagerConfig>(config.GetSection("AddinVersionManager"));
        services.Configure<LiborManagerConfig>(config.GetSection("LiborManager"));
        services.Configure<AssetReservesManagerConfig>(config.GetSection("AssetReservesManager"));
        services.Configure<BulkBluebooksManagerConfig>(config.GetSection("BulkBluebooksManager"));
        services.Configure<ExitProbabilityManagerConfig>(config.GetSection("ExitProbabilityManager"));
        services.Configure<LedgerManagerConfig>(config.GetSection("LedgerManager"));
        services.Configure<ValuationAttributesManagerConfig>(config.GetSection("ValuationAttributesManager"));
        services.Configure<ApplicationStatusManagerConfig>(config.GetSection("ApplicationStatusManager"));
        services.Configure<IrrBridgeManagerConfig>(config.GetSection("IrrBridgeManager"));
        services.Configure<TacticalPlanManagerConfig>(config.GetSection("TacticalPlanManager"));
        services.Configure<DebtManagerConfig>(config.GetSection("DebtManager"));
        services.Configure<TeamMemberManagerConfig>(config.GetSection("TeamMemberManager"));
        services.Configure<AssetInfoManagerConfig>(config.GetSection("AssetInfoManager"));
        services.Configure<UploadDisplayManagerConfig>(config.GetSection("UploadDisplayManager"));
        services.Configure<AdminManagerConfig>(config.GetSection("AdminManager"));
        services.Configure<FieldDictionaryManagerConfig>(config.GetSection("FieldDictionaryManager"));
        services.Configure<ApplicationManagerConfig>(config.GetSection("ApplicationManager"));
        services.Configure<ModelManagerConfig>(config.GetSection("ModelManager"));
        services.Configure<ReportingManagerConfig>(config.GetSection("ReportingManager"));
        services.Configure<ValuationsWorkflowManagerConfig>(config.GetSection("ValuationsWorkflowManager"));
        services.Configure<ValuationsSecondLevelApprovalManagerConfig>(config.GetSection("ValuationsSecondLevelApprovalManager"));
        services.Configure<ValuationApprovalEmailValidatorConfig>(config.GetSection("ValuationApprovalEmailValidator"));
        services.Configure<ValuationApprovalEmailProcessorConfig>(config.GetSection("ValuationApprovalEmailProcessor"));
        services.Configure<ValuationSecondLevelApprovalEmailProcessorConfig>(config.GetSection("ValuationSecondLevelApprovalEmailProcessor"));
        services.Configure<EmailCollectorConfig>(config.GetSection("EmailCollector"));
        services.Configure<DeferredSmtpServiceConfig>(config.GetSection("DeferredSmtpService"));
        services.Configure<DownloadProcessingJobConfig>(config.GetSection("DownloadProcessingJob"));
        services.Configure<DownloadProcessingJobTriggerConfig>(config.GetSection("DownloadProcessingJobTrigger"));
        services.Configure<BatchProcessingJobConfig>(config.GetSection("BatchProcessingJob"));
        services.Configure<BatchProcessingJobTriggerConfig>(config.GetSection("BatchProcessingJobTrigger"));
        services.Configure<ConversionProcessingJobConfig>(config.GetSection("ConversionProcessingJob"));
        services.Configure<TaskNotificationWatcherJobConfig>(config.GetSection("TaskNotificationWatcherJob"));
        services.Configure<TaskNotificationWatcherJobTriggerConfig>(config.GetSection("TaskNotificationWatcherJobTrigger"));
        services.Configure<TaskDueDateNotificationWatcherJobConfig>(config.GetSection("TaskDueDateNotificationWatcherJob"));
        services.Configure<TaskDueDateNotificationWatcherJobTriggerConfig>(config.GetSection("TaskDueDateNotificationWatcherJobTrigger"));
        services.Configure<HeartBeatNotificationJobConfig>(config.GetSection("HeartBeatNotificationJob"));
        services.Configure<HeartBeatNotificationJobTriggerConfig>(config.GetSection("HeartBeatNotificationJobTrigger"));
        services.Configure<SecurityCacheProcessingJobConfig>(config.GetSection("SecurityCacheProcessingJob"));
        services.Configure<SecurityCacheProcessingJobTriggerConfig>(config.GetSection("SecurityCacheProcessingJobTrigger"));
        services.Configure<ActiveDirectoryCacheProcessingJobConfig>(config.GetSection("ActiveDirectoryCacheProcessingJob"));
        services.Configure<ActiveDirectoryCacheProcessingJobAppStartTriggerConfig>(config.GetSection("ActiveDirectoryCacheProcessingJobAppStartTrigger"));
        services.Configure<ActiveDirectoryCacheProcessingJobDailyRefreshTriggerConfig>(config.GetSection("ActiveDirectoryCacheProcessingJobDailyRefreshTrigger"));
        services.Configure<DeferredSmtpServiceJobConfig>(config.GetSection("DeferredSmtpServiceJob"));
        services.Configure<DeferredSmtpServiceTriggerConfig>(config.GetSection("DeferredSmtpServiceTrigger"));
        services.Configure<UserProvisioningJobConfig>(config.GetSection("UserProvisioningJob"));
        services.Configure<UserProvisioningJobTriggerConfig>(config.GetSection("UserProvisioningJobTrigger"));
        services.Configure<QuartzSchedulerFactoryConfig>(config.GetSection("QuartzSchedulerFactory"));
        services.Configure<EventNotificationMangerConfig>(config.GetSection("EventNotificationManger"));
        services.Configure<TaskManagerConfig>(config.GetSection("TaskManager"));
        services.Configure<TaskScheduleManagerConfig>(config.GetSection("TaskScheduleManager"));
        services.Configure<ValuationReconciliationManagerConfig>(config.GetSection("ValuationReconciliationManager"));
        services.Configure<TaskTypeManagerConfig>(config.GetSection("TaskTypeManager"));
        services.Configure<HeartBeatManagerConfig>(config.GetSection("HeartBeatManager"));

        return services;
    }
}
