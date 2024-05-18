using UndertaleModLib;
using UndertaleModLib.Decompiler;

namespace YAMS_LIB.patches;

public class ColoredLogBook
{
    public static void Apply(UndertaleData gmData, GlobalDecompileContext decompileContext, SeedObject seedObject)
    {
        gmData.Scripts.AddScript("complex_text",
            """
            var textqueue, textlength, currpos, temppos, currchar, codechar, codeword, printxstart, printystart, printx, printy, edge, textcol, temptext, tempchar, loop1, loop2, lineheight, newline, prevtextcol, colval;
            printxstart = argument0
            printystart = argument1
            printx = printxstart
            printy = printystart
            textqueue = argument2
            textlength = string_length(textqueue)
            currpos = 1
            temppos = 1
            currchar = ""
            codechar = ""
            codeword = ""
            lineheight = argument3
            edge = printxstart + argument4
            textcol = draw_get_colour()
            prevtextcol = textcol
            colval[2] = 0
            colval[1] = 0
            colval[0] = 0
            loop1 = 0
            loop2 = 0
            newline = 0
            while (currpos <= (textlength))
            {
                newline = 0
                currchar = string_char_at(textqueue, currpos)
                if (currchar == " ")
                {
                    temppos = currpos + 1
                    temptext = ""
                    loop1 = 1
                    while (loop1 == 1)
                    {
                        tempchar = string_char_at(textqueue, temppos)
                        if (tempchar == "{")
                        {
                            loop2 = 1
                            while (loop2 == 1)
                            {
                                if (tempchar == "}")
                                    loop2 = 0

                                temppos++
                                tempchar = string_char_at(textqueue, temppos)
                                if temppos >= (textlength)
                                {
                                    loop1 = 0
                                    loop2 = 0
                                }
                            }
                        }
                        temptext += tempchar
                        if (temppos > (textlength) || tempchar == " " || tempchar == "#")
                            loop1 = 0
                        else
                            temppos++
                    }
                    if ((printx + string_width(temptext)) > edge)
                    {
                    //Set currchar to blank so the space isn't drawn after the new line.
                        currchar = ""
                        newline = 1
                    }
                }
                else if (currchar == "#")
                {
                    currchar = ""
                    newline = 1
                }
                else if (currchar == "{")
                {
                    currchar = ""
                    codeword = ""
                    loop1 = 1
                    loop2 = 1
                    while (currpos < (textlength + 1) && loop1 == 1)
                    {
                        currpos++
                        codechar = string_char_at(textqueue, currpos)
                        if (codechar == "}")
                            loop1 = 0
                        // CRASH if bracket is open to end of string
                        else if (codechar == "")
                        {
                            show_error("OPEN BRACKET", true)
                        }
                        else if (codechar == "C")
                        {
                            temppos = 2
                            repeat (3)
                            {
                                codeword = ""
                                repeat (3)
                                {
                                    currpos++
                                    codechar = string_char_at(textqueue, currpos)
                                    // CRASH if not a number.
                                    if (ord(codechar) < 48 || ord(codechar) > 57)
                                    {
                                        show_error("COLOR CODE: EXPECTED A NUMBER, GOT `" + codechar + "`", true)
                                    }
                                    codeword += codechar
                                }
                                colval[temppos] = min(real(codeword), 255)
                                temppos -= 1
                            }
                            textcol = make_colour_rgb(colval[2], colval[1], colval[0])
                        }
                        else
                        {
                            codeword += codechar
                            if (loop2 == 1)
                            {
                                //Default color set here, before the switch. Adding a Default case in the switch changed some valid colors to be white.
                                textcol = c_white
                                switch codeword
                                {
                                    case "aqua":
                                        textcol = c_aqua
                                        loop2 = 0
                                        break
                                    case "black":
                                        textcol = c_black
                                        loop2 = 0
                                        break
                                    case "blue":
                                        textcol = c_blue
                                        loop2 = 0
                                        break
                                    case "dkgray":
                                        textcol = c_dkgray
                                        loop2 = 0
                                        break
                                    case "fuchsia":
                                        textcol = c_fuchsia
                                        loop2 = 0
                                        break
                                    case "gray":
                                        textcol = c_gray
                                        loop2 = 0
                                        break
                                    case "green":
                                        textcol = c_green
                                        loop2 = 0
                                        break
                                    case "lime":
                                        textcol = c_lime
                                        loop2 = 0
                                        break
                                    case "ltgray":
                                        textcol = c_ltgray
                                        loop2 = 0
                                        break
                                    case "maroon":
                                        textcol = c_maroon
                                        loop2 = 0
                                        break
                                    case "navy":
                                        textcol = c_navy
                                        loop2 = 0
                                        break
                                    case "olive":
                                        textcol = c_olive
                                        loop2 = 0
                                        break
                                    case "orange":
                                        textcol = c_orange
                                        loop2 = 0
                                        break
                                    case "purple":
                                        textcol = c_purple
                                        loop2 = 0
                                        break
                                    case "red":
                                        textcol = c_red
                                        loop2 = 0
                                        break
                                    case "silver":
                                        textcol = c_silver
                                        loop2 = 0
                                        break
                                    case "teal":
                                        textcol = c_teal
                                        loop2 = 0
                                        break
                                    case "white":
                                        textcol = c_white
                                        loop2 = 0
                                        break
                                    case "yellow":
                                        textcol = c_yellow
                                        loop2 = 0
                                        break
                                }

                            }
                        }
                    }
                }
                // CRASH if right bracket has no corresponding left bracket.
                else if (currchar == "}")
                    show_error("UNEXPECTED END BRACKET AT Pos:" + string(currpos), true)
                else if ((printx + string_width(currchar)) > edge)
                    newline = 1
                if (newline == 1)
                {
                    printx = printxstart
                    printy += lineheight
                }
                draw_set_colour(textcol)
                //Only draw the current character if it is not blank
                if (currchar != "")
                {
                    draw_text(printx, printy, currchar)
                    printx += string_width(currchar)
                }
                currpos++
            }
            //Reset draw colour to value from before this script ran.
            draw_set_colour(prevtextcol)

            """);

        gmData.Scripts.AddScript("get_returned_text_of_complex_text",
            """
            var tempWord, tempChar, openedBracket, textQueue, textLength, currPos;
            textQueue = argument0;
            textLength = string_length(textQueue);
            currPos = 0;
            tempWord = "";
            tempChar = "";
            openedBracket = false;
            while (currPos <= (textLength))
            {
                tempChar = string_char_at(textQueue, currPos);
                // Technically should error if there's something like "{red{pink{}}" but I don't care, since complex_text would error on that.
                if (tempChar == "{")
                    openedBracket = true;

                if (!openedBracket)
                    tempWord += tempChar;

                if (tempChar == "}")
                    openedBracket = false;

                currPos++;
            }
            return tempWord;
            """);

        gmData.Code.ByName("gml_Object_oLogScreenControl_Other_10").ReplaceGMLInCode("draw_text_ext", "complex_text");
        gmData.Code.ByName("gml_Object_oLogScreenControl_Other_10").ReplaceGMLInCode("string_height_ext(logtext", "string_height_ext(get_returned_text_of_complex_text(logtext)");
        gmData.Code.ByName("gml_Object_oLogScreenControl_Other_11").ReplaceGMLInCode("draw_text_ext", "complex_text");
    }
}
