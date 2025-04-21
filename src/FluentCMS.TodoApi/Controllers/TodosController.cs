using FluentCMS.Core.Repositories.Abstractions;
using FluentCMS.TodoApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace FluentCMS.TodoApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TodosController : ControllerBase
{
    private readonly IBaseEntityRepository<Todo> _repository;
    private readonly ILogger<TodosController> _logger;

    public TodosController(IBaseEntityRepository<Todo> repository, ILogger<TodosController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    // GET: api/todos
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TodoResponseDto>>> GetTodos()
    {
        var todos = await _repository.GetAll();
        var todoResponses = todos.Select(MapToResponseDto);
        return Ok(todoResponses);
    }

    // GET: api/todos/5
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TodoResponseDto>> GetTodo(Guid id)
    {
        try
        {
            var todo = await _repository.GetById(id);
            return Ok(MapToResponseDto(todo));
        }
        catch (EntityNotFoundException)
        {
            return NotFound();
        }
    }

    // POST: api/todos
    [HttpPost]
    public async Task<ActionResult<TodoResponseDto>> CreateTodo(TodoCreateDto todoDto)
    {
        var todo = new Todo
        {
            Title = todoDto.Title,
            Description = todoDto.Description,
            IsCompleted = todoDto.IsCompleted,
            DueDate = todoDto.DueDate,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _repository.Add(todo);
        return CreatedAtAction(nameof(GetTodo), new { id = created.Id }, MapToResponseDto(created));
    }

    // PUT: api/todos/5
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateTodo(Guid id, TodoUpdateDto todoDto)
    {
        try
        {
            var existingTodo = await _repository.GetById(id);

            existingTodo.Title = todoDto.Title;
            existingTodo.Description = todoDto.Description;
            existingTodo.IsCompleted = todoDto.IsCompleted;
            existingTodo.DueDate = todoDto.DueDate;

            await _repository.Update(existingTodo);
            return NoContent();
        }
        catch (EntityNotFoundException)
        {
            return NotFound();
        }
    }

    // DELETE: api/todos/5
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteTodo(Guid id)
    {
        try
        {
            await _repository.Remove(id);
            return NoContent();
        }
        catch (EntityNotFoundException)
        {
            return NotFound();
        }
    }

    private static TodoResponseDto MapToResponseDto(Todo todo)
    {
        return new TodoResponseDto
        {
            Id = todo.Id,
            Title = todo.Title,
            Description = todo.Description,
            IsCompleted = todo.IsCompleted,
            DueDate = todo.DueDate,
            CreatedAt = todo.CreatedAt
        };
    }
}
