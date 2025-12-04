var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.ProjectManagement>("projectmanagement");
builder.AddProject<Projects.ProjectManagement_Adminstrator>("ProjectManagementAdminstrator");

builder.Build().Run();
