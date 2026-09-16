using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpsDesk.Core.Authorization;
using OpsDesk.Core.Services;
using OpsDesk.Web.Infrastructure;
using OpsDesk.Web.ViewModels.Customer;

namespace OpsDesk.Web.Controllers;

[Authorize(Policy = Permissions.Customer.View)]
public class CustomerController : Controller
{
    private readonly ICustomerService _customerService;
    private readonly ICurrentUserService _currentUser;

    public CustomerController(ICustomerService customerService, ICurrentUserService currentUser)
    {
        _customerService = customerService;
        _currentUser = currentUser;
    }

    // GET: /Customer
    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        const int pageSize = 15;
        var (items, total) = await _customerService.GetPagedAsync(search, page, pageSize);

        var vm = new CustomerListViewModel
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize,
            Search = search
        };

        return View(vm);
    }

    // GET: /Customer/Detail/5
    public async Task<IActionResult> Detail(int id)
    {
        var customer = await _customerService.GetDetailAsync(id);
        if (customer is null) return NotFound();

        return View(new CustomerDetailViewModel { Customer = customer });
    }

    // GET: /Customer/Create
    [Authorize(Policy = Permissions.Customer.Create)]
    public IActionResult Create()
    {
        return View(new CreateCustomerViewModel());
    }

    // POST: /Customer/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.Customer.Create)]
    public async Task<IActionResult> Create(CreateCustomerViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _customerService.CreateAsync(new CreateCustomerRequest(
            model.Name,
            model.Email,
            model.Phone,
            model.Company
        ), _currentUser.UserId);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error);
            return View(model);
        }

        TempData["SuccessMessage"] = $"Customer '{model.Name}' created successfully.";
        return RedirectToAction(nameof(Detail), new { id = result.Data });
    }

    // GET: /Customer/Edit/5
    [Authorize(Policy = Permissions.Customer.Update)]
    public async Task<IActionResult> Edit(int id)
    {
        var customer = await _customerService.GetByIdAsync(id);
        if (customer is null) return NotFound();

        var vm = new EditCustomerViewModel
        {
            Id = customer.Id,
            Name = customer.Name,
            Email = customer.Email,
            Phone = customer.Phone,
            Company = customer.Company
        };

        return View(vm);
    }

    // POST: /Customer/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.Customer.Update)]
    public async Task<IActionResult> Edit(int id, EditCustomerViewModel model)
    {
        if (id != model.Id) return BadRequest();

        if (!ModelState.IsValid)
            return View(model);

        var result = await _customerService.UpdateAsync(id, new UpdateCustomerRequest(
            model.Name,
            model.Email,
            model.Phone,
            model.Company
        ), _currentUser.UserId);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error);
            return View(model);
        }

        TempData["SuccessMessage"] = $"Customer '{model.Name}' updated successfully.";
        return RedirectToAction(nameof(Detail), new { id });
    }
}
