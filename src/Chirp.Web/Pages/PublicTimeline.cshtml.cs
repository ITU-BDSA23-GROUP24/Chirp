﻿using System.Runtime.CompilerServices;
using Chirp.Core;
using Chirp.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Chirp.Web.Pages;

public class PublicModel : PageModel
{
    private readonly ICheepRepository cheepRepository;
    public List<CheepViewModel> Cheeps { get; set; }

    [BindProperty(SupportsGet = true)]
    public int currentPage {get; set;}
    public int totalPages {get; set;}
    int pageSize {get; set;}

    public int navigationNumber {get; set;}

    public List<int> numbersToShow {get; set;}


    int count {get; set;}



    public PublicModel(ICheepRepository cheepRepository)
    {
        this.cheepRepository = cheepRepository;
        Cheeps = new List<CheepViewModel>();
        pageSize = 32;
        totalPages = 1;
        //The amount of pages that are shown between the "previous" and "next" button
        //Should always be odd, such that the current page can be in the center when relevant
        navigationNumber = 7;
        numbersToShow = new List<int>();
    }

    public async Task<ActionResult> OnGetAsync([FromQuery] int page)
    {
        if (page < 1) page = 1;

        currentPage = page;
        numbersToShow.Clear();

        try
        {
            IEnumerable<CheepViewModel> cheeps = await cheepRepository.GetPageOfCheeps(page, pageSize);
            Cheeps = cheeps.ToList();
            count = await cheepRepository.GetCheepCountAll();
            totalPages = count/pageSize;
            //Adds one extra page if the amount if cheeps is not perfectly divisible by the page size, where the remaining cheeps can be shown
            if (count%pageSize != 0){
                totalPages++;
            }
            int middleNumber = navigationNumber/2+1;
            if (currentPage-navigationNumber/2 < 1){
                for (int i = 1 ; i <= navigationNumber ; i++){
                    numbersToShow.Add(i);
                }
            }
            else if (currentPage + navigationNumber/2 > totalPages){
                for (int i = totalPages-navigationNumber; i <= totalPages ; i++){
                    numbersToShow.Add(i);
                }
            }
            else {
                for (int i = currentPage - navigationNumber/2; i <= currentPage + navigationNumber/2; i++){
                    numbersToShow.Add(i);
                }
            }
        }
        catch
        {
            //empty list of cheeps if there are no cheeps to show, this is caught by the cshtml and shown as "There are no cheeps here"
            Cheeps = new List<CheepViewModel>();
        }

        return Page();
    }
}