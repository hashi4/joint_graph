@setlocal
@rem .net 4.0�ȏ�̃R���p�C����K�X�w��
@rem ����Window10�ɓ�������Ă������(64bit)
@set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe

@rem PMX�G�f�B�^�̃��C�u�����ʒu���w��(pmx�G�f�B�^���C���X�g�[�������p�X\Lib)
@set PMXE_LIBPATH=..\Lib

@set LIBPATH=%PMXE_LIBPATH%\PEPlugin,%PMXE_LIBPATH%\SlimDX\x64
@set LIBS=PEPlugin.dll,SlimDX.dll

@set PLUGIN_NAME=joint_graph
@set SRC=%PLUGIN_NAME%.cs
@set TARGET=%PLUGIN_NAME%

@set TARGET=%TARGET%.dll

%CSC% /target:library /out:%TARGET% %SRC% /lib:%LIBPATH% /r:%LIBS%
@%endlocal
