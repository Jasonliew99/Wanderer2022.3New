using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//diddy head counting script for the fragments,
public class FragmentRevealManager : MonoBehaviour
{
    public static FragmentRevealManager instance;

    [Header("Reveal Order (LEVEL 1)")]
    public Sprite[] fragmentRevealOrder; // this shit is for the order of the fragments to be revealed
    private int revealedFragments = 0;

    private void Awake()
    {
        instance = this; // instance? nah man, THIS
    }

    // please where is the next fragment, im kinda homeless
    public Sprite RequestNextFragment()
    {
        if (revealedFragments >= fragmentRevealOrder.Length)//
            return null;

        Sprite next = fragmentRevealOrder[revealedFragments];
        revealedFragments++;

        return next;
    }
}
