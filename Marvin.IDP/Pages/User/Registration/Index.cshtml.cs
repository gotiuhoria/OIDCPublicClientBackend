using Duende.IdentityServer;
using Duende.IdentityServer.Services;
using IdentityModel;
using Marvin.IDP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Marvin.IDP.Pages.User.Registration;

[AllowAnonymous]
[SecurityHeaders]
public class Index : PageModel
{
    private readonly ILocalUserService _localUserService;
    private readonly IIdentityServerInteractionService _interaction;

    public Index(
        ILocalUserService localUserService,
        IIdentityServerInteractionService interaction)
    {
        _localUserService = localUserService ?? throw new ArgumentException(nameof(localUserService));
        _interaction = interaction ?? throw new ArgumentException(nameof(interaction));
    }

    [BindProperty]
    public InputModel Input { get; set; }
    
    public IActionResult OnGet(string returnUrl)
    {
        BuildModel(returnUrl);

        return Page();
    }
    
    private void BuildModel(string returnUrl)
    {
        Input = new InputModel
        {
            ReturnUrl = returnUrl
        };
    }

    public async Task<IActionResult> OnPost()
    {
        if (!ModelState.IsValid)
        {
            // something went wrong, show form with error
            BuildModel(Input.ReturnUrl);
            return Page();
        }

        // create user & claims
        var userToCreate = new Entities.User
        {
            UserName = Input.UserName,
            Subject = Guid.NewGuid().ToString(),
            Email = Input.Email,
            Active = false 
        };
        
        userToCreate.Claims.Add(new Entities.UserClaim()
        {
            Type = "email",
            Value = Input.Email
        });
        
        userToCreate.Claims.Add(new Entities.UserClaim()
        {
            Type = "country",
            Value = Input.Country
        });

        userToCreate.Claims.Add(new Entities.UserClaim()
        {
            Type = JwtClaimTypes.GivenName,
            Value = Input.GivenName
        });

        userToCreate.Claims.Add(new Entities.UserClaim()
        {
            Type = JwtClaimTypes.FamilyName,
            Value = Input.FamilyName
        });
        
        _localUserService.AddUser(userToCreate, Input.Password);
        await _localUserService.SaveChangesAsync();

        var activationLink = Url.PageLink("/user/activation/index",
            values: new { securityCode = userToCreate.SecurityCode });
        
        Console.WriteLine($"ActivationLink:{activationLink}", activationLink);

        return Redirect("~/User/ActivationCodeSent");

        // var isUser = new IdentityServerUser(userToCreate.Subject)
        // {
        //     DisplayName = userToCreate.UserName
        // };
        //
        // await HttpContext.SignInAsync(isUser);
        //
        // //continue with the flow
        // if (_interaction.IsValidReturnUrl(Input.ReturnUrl) || Url.IsLocalUrl(Input.ReturnUrl))
        // {
        //     return Redirect(Input.ReturnUrl);
        // }
        //
        // return Redirect("~/");
    }
}