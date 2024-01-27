using System.ComponentModel.DataAnnotations.Schema;
using MediatR;
using MediatR.Pipeline;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<IApiMarker>());
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<IApiMarker>();
    cfg.AddOpenBehavior(typeof(GenericBehavior<,>));
    cfg.AddOpenBehavior(typeof(GenericBehavior2<,>));
    cfg.AddBehavior<IPipelineBehavior<Ping,string>, SpecificBehavior>();
    cfg.AddBehavior<IPipelineBehavior<Ping,string>, SpecificBehavior2>();
    cfg.AddOpenRequestPreProcessor(typeof(GenericPreProcessor<>));
    cfg.AddOpenRequestPostProcessor(typeof(GenericPostProcessor<,>));
    cfg.AddRequestPreProcessor<SpecificPreProcessor>();
    cfg.AddRequestPostProcessor<SpecificPostProcessor>();
});

// builder.Services.AddDbContext<ExampleContext>(options =>
//     options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDbContext<ExampleContext>(
    (serviceProvider, options) => options
        .UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
        .AddInterceptors(serviceProvider.GetRequiredService<DispatchDomainEventsInterceptor>()));

builder.Services.AddTransient<DispatchDomainEventsInterceptor>();


var app = builder.Build();

app.MapGet("/", () => "Hello World!");
app.MapGet("/ping", (IMediator mediator) => mediator.Send(new Ping()));
app.MapGet("/oneway", (IMediator mediator) => mediator.Send(new OneWay()));
app.MapGet("/oneway2", (IMediator mediator) => mediator.Send(new OneWay2()));
app.MapGet("/notification", async (IMediator mediator) =>
{
    await mediator.Publish(new MyNotification());
    // At this point, all handlers will have been executed
});
app.MapPost("/customers", async (IMediator mediator, CreateCustomerRequest request) => await mediator.Send(request));


app.Run();

public interface IApiMarker 
{
    
}

public class Ping : IRequest<string>
{
}

public class PingHandler(ILogger<PingHandler> logger) : IRequestHandler<Ping, string>
{
    public Task<string> Handle(Ping request, CancellationToken cancellationToken)
    {
        logger.LogInformation("PingHandler invoked");
        // throw new DivideByZeroException();
        return Task.FromResult("Pong");
    }
}

public class OneWay : IRequest
{
}

public class OneWayHandler(ILogger<OneWayHandler> logger) : IRequestHandler<OneWay>
{
    public Task Handle(OneWay request, CancellationToken cancellationToken)
    {
        logger.LogInformation("OneWayHandler invoked");
        return Task.CompletedTask;
    }
}

public class OneWay2 : IRequest<Unit>
{
}

public class OneWay2Handler(ILogger<OneWay2Handler> logger) : IRequestHandler<OneWay2, Unit>
{
    public Task<Unit> Handle(OneWay2 request, CancellationToken cancellationToken)
    {
        logger.LogInformation("OneWay2Handler invoked");
        return Task.FromResult(Unit.Value);
    }
}

// This IRequestExceptionHandler handles DivideByZeroException for Ping request
public class RequestExceptionProcessorBehavior(ILogger<RequestExceptionProcessorBehavior> logger) : IRequestExceptionHandler<Ping, string, DivideByZeroException>
{
    public Task Handle(Ping request, DivideByZeroException exception, RequestExceptionHandlerState<string> state,
        CancellationToken cancellationToken)
    {
        logger.LogInformation($"RequestExceptionProcessorBehavior {exception}");
        // You can specify that the exception has been handled for stopping further actions processing
        // state.SetHandled("Ping handled");
        return Task.CompletedTask;
    }
}

// Actions will be called after IRequestExceptionHandler and if the exception is not handled
// Action for Ping and DivideByZeroException
public class RequestExceptionActionProcessorBehavior(ILogger<RequestExceptionActionProcessorBehavior> logger) : IRequestExceptionAction<Ping, DivideByZeroException>
{
    public Task Execute(Ping request, DivideByZeroException exception, CancellationToken cancellationToken)
    {
        logger.LogInformation($"RequestExceptionActionProcessorBehavior {exception}");
        return Task.CompletedTask;
    }
}

// Action for Ping and whatever exception
public class RequestExceptionActionProcessorBehavior2(ILogger<RequestExceptionActionProcessorBehavior2> logger) : IRequestExceptionAction<Ping, Exception>
{
    public Task Execute(Ping request, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogInformation($"RequestExceptionActionProcessorBehavior2 {exception}");
        return Task.CompletedTask;
    }
}

public class MyNotification : INotification
{
}

// Zero or more handlers for a notification
// Handler interfaces are contravariant https://github.com/jbogard/MediatR/wiki#polymorphic-dispatch 
public class NotificationHandler(ILogger<NotificationHandler> logger) : INotificationHandler<INotification>
{
    public Task Handle(INotification notification, CancellationToken cancellationToken)
    {
        logger.LogInformation($"Notification received in {nameof(NotificationHandler)}");
        return Task.CompletedTask;
    }
}

public class MyNotificationHandler(ILogger<MyNotificationHandler> logger) : INotificationHandler<MyNotification>
{
    public Task Handle(MyNotification notification, CancellationToken cancellationToken)
    {
        logger.LogInformation($"Notification received in {nameof(MyNotificationHandler)}");
        return Task.CompletedTask;
    }
}

public class MyNotification2Handler(ILogger<MyNotification2Handler> logger) : INotificationHandler<MyNotification>
{
    public Task Handle(MyNotification notification, CancellationToken cancellationToken)
    {
        logger.LogInformation($"Notification received in {nameof(MyNotification2Handler)}");
        return Task.CompletedTask;
    }
}


// Specific pre-processor for Ping request
public class SpecificPreProcessor(ILogger<SpecificPreProcessor> logger) : IRequestPreProcessor<Ping>
{
    public Task Process(Ping request, CancellationToken cancellationToken)
    {
        logger.LogInformation($"SpecificPreProcessor {request}");
        return Task.CompletedTask;
    }
}

// Generic pre-processor for all requests
public class GenericPreProcessor<TRequest>(ILogger<GenericPreProcessor<TRequest>> logger)
    : IRequestPreProcessor<TRequest>
{
    public Task Process(TRequest request, CancellationToken cancellationToken)
    {
        logger.LogInformation($"GenericPreProcessor {request}");
        return Task.CompletedTask;
    }
}

// Generic behavior for all requests and responses
public class GenericBehavior<TRequest, TResponse>(ILogger<GenericBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        logger.LogInformation($"GenericBehavior before {typeof(TRequest).Name}");
        TResponse response = await next();
        logger.LogInformation($"GenericBehavior after {typeof(TResponse).Name}");
        return response;
    }
}

// Generic behavior for all requests and responses
public class GenericBehavior2<TRequest, TResponse>(ILogger<GenericBehavior2<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        logger.LogInformation($"GenericBehavior2 before {typeof(TRequest).Name}");
        TResponse response = await next();
        logger.LogInformation($"GenericBehavior2 after {typeof(TResponse).Name}");
        return response;
    }
}

// Specific behavior for Ping request and string response
public class SpecificBehavior(ILogger<SpecificBehavior> logger) : IPipelineBehavior<Ping, string>
{
    public async Task<string> Handle(Ping request, RequestHandlerDelegate<string> next,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("SpecificBehavior before");
        string response = await next();
        logger.LogInformation("SpecificBehavior after");
        return response;
    }
}

// Specific behavior for Ping request and string response
public class SpecificBehavior2(ILogger<SpecificBehavior2> logger) : IPipelineBehavior<Ping, string>
{
    public async Task<string> Handle(Ping request, RequestHandlerDelegate<string> next,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("SpecificBehavior2 before");
        string response = await next();
        logger.LogInformation("SpecificBehavior2 after");
        return response;
    }
}

// Specific post-processor for Ping request and string response
public class SpecificPostProcessor(ILogger<SpecificPostProcessor> logger) : IRequestPostProcessor<Ping, string>
{
    public Task Process(Ping request, string response, CancellationToken cancellationToken)
    {
        logger.LogInformation($"SpecificPostProcessor {response}");
        return Task.FromResult(response);
    }
}

// Generic post-processor for all requests and responses
public class GenericPostProcessor<TRequest, TResponse>(ILogger<GenericPostProcessor<TRequest, TResponse>> logger)
    : IRequestPostProcessor<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public Task Process(TRequest request, TResponse response, CancellationToken cancellationToken)
    {
        logger.LogInformation($"GenericPostProcessor {request} {response}");
        return Task.CompletedTask;
    }
}

public class ExampleContext : DbContext
{
    public ExampleContext(DbContextOptions<ExampleContext> options) : base(options)
    {
    }

    public DbSet<Customer> Customers { get; set; }
}

public class Customer: BaseEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    
    public void Update(string name, string? description)
    {
        Name = name;
        Description = description;
        AddDomainEvent(new CustomerUpdatedEvent(this));
    }
}

public class CreateCustomerRequest : IRequest<CreateCustomerResponse>
{
    public string Name { get; set; }
    public string? Description { get; set; }
}

public class CreateCustomerResponse
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
}

public class CreateCustomerHandler(ExampleContext context)
    : IRequestHandler<CreateCustomerRequest, CreateCustomerResponse>
{
    public async Task<CreateCustomerResponse> Handle(CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        var customer = new Customer();
        customer.Update(request.Name, request.Description);
        await context.Customers.AddAsync(customer, cancellationToken: cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return new CreateCustomerResponse
        {
            Id = customer.Id,
            Name = customer.Name,
            Description = customer.Description
        };
    }
}

public class DispatchDomainEventsInterceptor(IPublisher publisher, ILogger<DispatchDomainEventsInterceptor> logger) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        logger.LogInformation("SavingChanges");
        DispatchDomainEvents(eventData.Context).GetAwaiter().GetResult();
        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result,
        CancellationToken cancellationToken = new())
    {
        logger.LogInformation("SavedChangesAsync");
        await DispatchDomainEvents(eventData.Context);
        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }


    private async Task DispatchDomainEvents(DbContext? context)
    {
        if (context is null)
        {
            return;
        }
        
        var entities = context.ChangeTracker
            .Entries<BaseEntity>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity);

        var domainEvents = entities
            .SelectMany(e => e.DomainEvents)
            .ToList();

        entities.ToList().ForEach(e => e.ClearDomainEvents());

        foreach (var domainEvent in domainEvents)
            await publisher.Publish(domainEvent);
    }
} 

public abstract class BaseEvent : INotification
{
}

public abstract class BaseEntity
{
    public int Id { get; set; }

    private readonly List<BaseEvent> _domainEvents = new();

    [NotMapped]
    public IEnumerable<BaseEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void AddDomainEvent(BaseEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void RemoveDomainEvent(BaseEvent domainEvent)
    {
        _domainEvents.Remove(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}

public class CustomerUpdatedEvent : BaseEvent
{
    public CustomerUpdatedEvent(Customer customer)
    {
        Customer = customer;
    }

    public Customer Customer { get; }
}

public class CustomerUpdatedEventHandler(ILogger<CustomerUpdatedEventHandler> logger) : INotificationHandler<CustomerUpdatedEvent>
{
    public Task Handle(CustomerUpdatedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation($"CustomerUpdatedEvent handled {notification.Customer}");
        return Task.CompletedTask;
    }
}
