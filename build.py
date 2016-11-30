import os
import shutil
import argparse
import subprocess

parser = argparse.ArgumentParser(description='config for WiX build')
parser.add_argument('config', type=str, help='config type for WiX build')
args = parser.parse_args()
config = args.config

solution_dir = os.path.dirname(os.path.realpath(__file__))
bin = os.path.join(solution_dir, 'bin')
config_dir = os.path.join(bin, 'config')
build_dir = os.path.join(bin, config)
wix_proj_dir = os.path.join(solution_dir, 'WixProject1')
wix_proj = os.path.join(wix_proj_dir, 'WixProject1.wixproj')
heat = 'C:\Program Files (x86)\WiX Toolset v3.10\\bin\heat.exe'

def build():
    conf = os.path.join(build_dir, 'config')
    if not os.path.exists(conf):
        os.mkdir(conf)

    src_files = os.listdir(config_dir)
    for file_name in src_files:
        full_file_name = os.path.join(config_dir, file_name)
        if (os.path.isfile(full_file_name)):
            shutil.copy(full_file_name, os.path.join(build_dir, 'config'))

    # move Service.exe from bin so it is not harvested
    shutil.move(os.path.join(build_dir, 'Service.exe'), solution_dir)
    #arg1 = 'dir %s' % build_dir
    #arg2 = 'dr INSTALLFOLDER'
    #arg3 = 'cg ProductComponents'
    #arg4 = '-gg'
    #arg5 = '-sfrag'
    #arg6 = '-srd'
    #arg7 = '-suid'
    #arg8 = '-out %s' % os.path.join(wix_proj_dir, 'HeatFile.wxs')
    #print(arg8)
    #h = subprocess.call([heat, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8], shell=True);
    msbuild = 'C:\Windows\Microsoft.NET\Framework\\v4.0.30319\MSBuild.exe'
    arg1 = '/t:Rebuild'
    arg2 = '/p:Configuration=%s' % config
    #p = subprocess.call([msbuild, wix_proj, arg1, arg2], shell=True)

if __name__ == "__main__":
    build()