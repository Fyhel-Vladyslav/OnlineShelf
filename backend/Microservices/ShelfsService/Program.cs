//using ShelfsService.Extentions;

//var builder = WebApplication.CreateBuilder(args);

//// Add services to the container.

//builder.Services
//    .AddInfrastructure(builder.Configuration, builder.Environment)
//    //.AddAuthorization(builder.Configuration, builder.Environment)
//;


//var app = builder.Build();

//// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}

//app.UseHttpsRedirection();

//app.UseAuthorization();

//app.MapControllers();

//app.Run();



using ShelfsService.Extentions;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddInfrastructure(builder.Configuration, builder.Environment)
   // .AddAuthorization(builder.Configuration, builder.Environment)
;

var app = builder.Build();

app.UseShelfsInfrastucture();

app.Run();