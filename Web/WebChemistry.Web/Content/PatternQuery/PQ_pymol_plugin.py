#!/usr/bin/env python
# -*- coding: iso-8859-2 -*-

from Tkinter import *
import tkMessageBox
import tkFileDialog
import Pmw
from pymol import cmd
from pymol import preset
import os.path
import urllib2
import re

def __init__(self):
        self.menuBar.addmenuitem('Plugin', 'command',
                                 'Launch PQ Loader',
                                 label='PatternQuery Loader',
                                 command = lambda s=self: PQ(s))

class PQ:
    def __init__(self, app):
        root = Toplevel(app.root)
        root.title('PatternQuery Loader')
        parent = root
        mainframe = Frame(parent, width=300, height=500)
        mainframe.pack(fill = 'both', expand = 1)

        balloon = Pmw.Balloon(mainframe)

        notebook = Pmw.NoteBook(mainframe)
        notebook.pack(fill = 'both', expand = 1)

        mainpage = notebook.add('Visualize/Load patterns')
        notebook.tab('Visualize/Load patterns').focus_set()
        groupD = Pmw.Group(mainpage, tag_text = 'Which PDB ids should be loaded?')

        radSel = Pmw.RadioSelect(mainpage,
                buttontype = 'radiobutton',
                labelpos = 'nw',
                label_text = 'Download PDB structures',
                frame_borderwidth = 2,
                frame_relief = 'ridge',
        )
        radSel.pack(fill = 'x', padx = 10, pady = 10)

        for text in ('None', 'User Defined', 'From Pattern Filename'):
            radSel.add(text)
        radSel.invoke('None')



        groupD.pack(fill = 'both', expand = 1, padx = 2, pady = 2)
        ids = Pmw.EntryField(groupD.interior(),
            labelpos = 'w',
            label_text = 'PDB ids:')
        ids.pack(fill = 'x', padx = 20, pady = 10)

        SelectPatternsButton = Button(mainpage, text = 'Select & display patterns', command= lambda : self.ProcessRequest(ids.getvalue(), radSel.getvalue()))
        SelectPatternsButton.pack(fill = 'x')


        balloon.bind(radSel, 'Optionally, you can download PDBx structures the patterns originated from.\n\
                    None - No structures will be downloaded.\n\
                    User Defined - Specify a list of 4-letter ids in a textbox below.\n\
                    From Pattern Filename - Structures will be downloaded based on the names of patterns.')
        balloon.bind(groupD, 'Provide a comma separated list of PDB ids (1tqn, 2hhb, ...)')

        about = notebook.add('About')

        Guide = Label(about, relief = 'sunken',anchor=W, justify=LEFT, padx = 10, pady = 10)
        Guide.pack(fill='both')
        Guide.configure(fg='black')
        Guide.configure(text ="PatternQuery Loader\n\nfor information on how to use the service,\nplease follow tooltips.\n\nBug report: xpravda@ncbr.muni.cz\n\nhttp://webchem.ncbr.muni.cz")



        balloon.bind(SelectPatternsButton, 'Once you are done with structure selection,\nclick the button and select PQ patterns from your HDD.')

        notebook.setnaturalsize()


    def DownloadStructures(self, ids):
        result = []
        for id in ids:
            try:
                url = 'http://www.ebi.ac.uk/pdbe/entry-files/download/' + id.lower() + '.cif'
                f = urllib2.urlopen(url)
                result.append(id)
                with open(id + '.cif', "wb") as code:
                    code.write(f.read())
            except:
                pass
        return result



    def VisualizeStructures(self, ids):
        print("viz method")
        print(ids)
        for i in ids:
            print(i)
            cmd.load(i + '.cif')
            cmd.hide('everything', i)
            cmd.show('cartoon', i)
            cmd.set("cartoon_color", "gray80", i)



    def VisualizePatterns(self, filelist):
        for file in filelist:
            selection = os.path.splitext(os.path.basename(file))[0]
            cmd.load(file)

            preset.ball_and_stick(selection, mode=1)
            cmd.set('sphere_color', 'gray30', 'ele C and ' + selection)
            cmd.set('sphere_color', 'firebrick', 'ele O and ' + selection)
            cmd.set('sphere_color', 'marine', 'ele N and ' + selection)



    def ProcessRequest(self, ids, mode):
        selectedFiles = tkFileDialog.askopenfilename(title='Select PQ patern files', filetypes=[('*.pdb pattern files','.pdb')], multiple=True).encode("utf-8")

        if '{' in selectedFiles:
            filelist = re.split(r'[{}]', selectedFiles)
            filelist = filter(lambda l: len(l) > 2, filelist)
        else:
            filelist = selectedFiles.split()


        if mode == 'From Pattern Filename':
            structures = set(map(lambda l: os.path.splitext(os.path.basename(l))[0][0:4], filelist))
        elif mode == 'User Defined':
            structures = re.findall('[a-zA-Z0-9]{4}', ids)
        else:
            structures = []
        try:
            downloaded = self.DownloadStructures(structures)
            print(downloaded)
            self.VisualizeStructures(downloaded)
            self.VisualizePatterns(filelist)
        except Exception as e:
            tkMessageBox.showinfo('Error has occured', str(e))
