/*
    by korenkonder
    GitHub/GitLab: korenkonder
*/

using System;
using System.IO;
using System.Globalization;
using System.Collections.Generic;

namespace ARBConverter
{
    public class ARBConverter
    {
        public const int MAX_CLIP_PLANES = 8;
        public const int MAX_DRAW_BUFFERS_ARB = 8;
        public const int MAX_LIGHTS = 8;
        public const int MAX_PALETTE_MATRICES_ARB = 32;
        public const int MAX_PROGRAM_ENV_PARAMETERS_ARB = 256;
        public const int MAX_PROGRAM_LOCAL_PARAMETERS_ARB = 1024;
        public const int MAX_PROGRAM_PARAMETER_BUFFER_BINDINGS_NV = 14;
        public const int MAX_PROGRAM_PARAMETER_BUFFER_SIZE_NV = 16384;
        public const int MAX_PROGRAM_MATRICES_ARB = 8;
        public const int MAX_TEXTURE_COORDS_ARB = 8;
        public const int MAX_TEXTURE_IMAGE_UNITS_ARB = 32;
        public const int MAX_TEXTURE_UNITS_ARB = 4;
        public const int MAX_UNIFORM_BLOCK_SIZE = 65536;
        public const int MAX_VERTEX_ATTRIBS_ARB = 16;
        public const int MAX_VERTEX_UNITS_ARB = 4;

        public static System.Text.Encoding Encoding = System.Text.Encoding.ASCII;
        public const string Tab = "    ";

        private Dictionary<INameAttrib, IType> attrib;
        private Dictionary<INameOutput, IType> output;
        private Dictionary<INameParam, IType> param;
        private List<IInstruction> inst;
        private Dictionary<string, string> rename;
        private Dictionary<string, string> renameAlt;
        private List<Temp> temp;
        private List<string> label;
        private Mode mode;
        private bool hasCC;
        private Dictionary<string, string> match;
        private Dictionary<string, Modifier> matchMod;
        private bool useInclude;
        private int instLevel;

        private Stream s;

        public void Convert(string f, bool useInclude = true)
        {
            string f0 = Path.GetFileName(f);
            Console.Title = f0;
            if (!File.Exists(f))
                return;

            this.useInclude = useInclude;
            if (useInclude)
            {
                if (!File.Exists(f.Replace(f0, "sharedVert.glsl")))
                {
                    s = File.OpenWrite(f.Replace(f0, "sharedVert.glsl"));
                    s.SetLength(0);
                    WriteSharedVertGLSL();
                    s.Close();
                }

                if (!File.Exists(f.Replace(f0, "sharedFrag.glsl")))
                {
                    s = File.OpenWrite(f.Replace(f0, "sharedFrag.glsl"));
                    s.SetLength(0);
                    WriteSharedFragGLSL();
                    s.Close();
                }

                if (!File.Exists(f.Replace(f0, "shared.glsl")))
                {
                    s = File.OpenWrite(f.Replace(f0, "shared.glsl"));
                    s.SetLength(0);
                    WriteSharedGLSL();
                    s.Close();
                }
            }

            string[] s0 = File.ReadAllLines(f);
            int c, i, i0;
            c = s0.Length;

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (i = 0; i < c; i++)
                if (s0[i].StartsWith("!!"))
                    sb.Append(s0[i] + ";");
                else
                    sb.Append(s0[i].Split('#')[0]);

            string str = sb.ToString();
            sb.Clear();
            sb = null;

            s0 = str.Split(';');
            i0 = s0.Length;

            //string d = $"H:\\shaders\\{Path.GetFileNameWithoutExtension(f)}";
            string d = f.Substring(0, f.Length - Path.GetExtension(f).Length);
            try
            {
                ParseARBFile(s0, i0);

                switch (mode)
                {
                    case Mode.FragmentProgram: d += ".frag"; break;
                    case Mode.  VertexProgram: d += ".vert"; break;
                    default: return;
                }
                if (File.Exists(d + "fail")) File.Delete(d + "fail");
                s = File.OpenWrite(d);
                s.SetLength(0);

                try
                {
                    WriteProgram();
                    s.Close();
                }
                catch (Exception e)
                {
                    s.Close();
                    Console.WriteLine($"Got an excepton \"{e}\" while converting file \"{Path.GetFileName(f)}\"");

                    if (File.Exists(d + "fail")) File.Delete(d + "fail");
                    if (File.Exists(d)) File.Move(d, d + "fail");
                    else File.Create(d + "fail");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Got an excepton \"{e}\" while converting file \"{Path.GetFileName(f)}\"");
                switch (mode)
                {
                    case Mode.FragmentProgram: d += ".frag"; break;
                    case Mode.  VertexProgram: d += ".vert"; break;
                    default: return;
                }

                if (File.Exists(d + "fail")) File.Delete(d + "fail");
                if (File.Exists(d)) File.Move(d, d + "fail");
                else File.Create(d + "fail");
            }
        }

        private void WriteSharedVertGLSL()
        {
            write($"layout (location = {0}) in {"vec4"} {"aPos"};");
            write($"layout (location = {1}) in {"vec4"} {"aWeight"};");
            write($"layout (location = {2}) in {"vec4"} {"aNormal"};");
            write($"layout (location = {3}) in {"vec4"} {"aColor0"};");
            write($"layout (location = {4}) in {"vec4"} {"aColor1"};");
            write($"layout (location = {5}) in {"float"} {"aFogCoord"};");
            for (int i = 0; i < MAX_TEXTURE_COORDS_ARB; i++)
                write($"layout (location = {8 + i}) in {"vec4"} {$"aTexCoord{i}"};");
            //for (int i = 0; i < MAX_VERTEX_UNITS_ARB; i++)
            //    write($"layout (location = {location++}) in {"vec4"} {$"aWeight{i}"};");
            for (int i = 0; i < MAX_VERTEX_ATTRIBS_ARB; i++)
                write($"layout (location = {i}) in {"vec4"} {$"aAttrib{i}"};");
            write("");

            write($"out vec4 {$"fAttrib[{MAX_VERTEX_ATTRIBS_ARB}]"};");
            write($"out vec4 {"fColor[2]"};");
            write($"out vec4 {"fColorFront[2]"};");
            write($"out vec4 {"fColorBack[2]"};");
            write($"out vec4 {"fFogCoord"};");
            write($"out vec4 {$"fTexCoord[{MAX_TEXTURE_COORDS_ARB}]"};");
            write("");

            write($"#define {"MAX_PROGRAM_LOCAL_PARAMETERS_ARB"} {MAX_PROGRAM_LOCAL_PARAMETERS_ARB}");
            write("");

            write($"layout(location = {MAX_TEXTURE_IMAGE_UNITS_ARB}) uniform vec4" +
                " PrgVertLocal[MAX_PROGRAM_LOCAL_PARAMETERS_ARB];");

            void write(string str)
            {
                byte[] buf = Encoding.GetBytes(str);
                s.Write(buf, 0, buf.Length);
                s.WriteByte((byte)'\n');
            }
        }

        private void WriteSharedFragGLSL()
        {
            int location = 0;
            for (int i = 0; i < MAX_DRAW_BUFFERS_ARB; i++)
                write($"layout (location = {location++}) out vec4 oColor{i};");
            write("");

            write($"in vec4 {$"fAttrib[{MAX_VERTEX_ATTRIBS_ARB}]"};");
            write($"in vec4 {"fColor[2]"};");
            write($"in vec4 {"fColorFront[2]"};");
            write($"in vec4 {"fColorBack[2]"};");
            write($"in vec4 {"fFogCoord"};");
            write($"in vec4 {$"fTexCoord[{MAX_TEXTURE_COORDS_ARB}]"};");
            write("");

            write($"#define {"MAX_PROGRAM_LOCAL_PARAMETERS_ARB"} {MAX_PROGRAM_LOCAL_PARAMETERS_ARB}");
            write("");

            write($"layout(location = {MAX_TEXTURE_IMAGE_UNITS_ARB}) uniform vec4" +
                " PrgFragLocal[MAX_PROGRAM_LOCAL_PARAMETERS_ARB];");

            void write(string str)
            {
                byte[] buf = Encoding.GetBytes(str);
                s.Write(buf, 0, buf.Length);
                s.WriteByte((byte)'\n');
            }
        }

        private void WriteSharedGLSL()
        {
            write($"#define {"MAX_CLIP_PLANES"} {MAX_CLIP_PLANES}");
            write($"#define {"MAX_LIGHTS"} {MAX_LIGHTS}");
            write($"#define {"MAX_PALETTE_MATRICES_ARB"} {MAX_PALETTE_MATRICES_ARB}");
            write($"#define {"MAX_PROGRAM_ENV_PARAMETERS_ARB"} {MAX_PROGRAM_ENV_PARAMETERS_ARB}");
            write($"#define {"MAX_PROGRAM_PARAMETER_BUFFER_SIZE_NV"} {MAX_PROGRAM_PARAMETER_BUFFER_SIZE_NV}");
            write($"#define {"MAX_PROGRAM_MATRICES_ARB"} {MAX_PROGRAM_MATRICES_ARB}");
            write($"#define {"MAX_TEXTURE_COORDS_ARB"} {MAX_TEXTURE_COORDS_ARB}");
            write($"#define {"MAX_TEXTURE_IMAGE_UNITS_ARB"} {MAX_TEXTURE_IMAGE_UNITS_ARB}");
            write($"#define {"MAX_TEXTURE_UNITS_ARB"} {MAX_TEXTURE_UNITS_ARB}");
            write($"#define {"MAX_UNIFORM_BLOCK_SIZE"} {MAX_UNIFORM_BLOCK_SIZE}");
            write($"#define {"MAX_VERTEX_UNITS_ARB"} {MAX_VERTEX_UNITS_ARB}");
            write("");

            write("struct ClipStruct");
            write("{");
            write($"{Tab}vec4 Plane;");
            write("};");
            write("");

            write("struct DepthStruct");
            write("{");
            write($"{Tab}vec4 Range;");
            write("};");
            write("");

            write("struct FogStruct");
            write("{");
            write($"{Tab}vec4 Color;");
            write($"{Tab}vec4 Params;");
            write("};");
            write("");

            write("struct LightStruct");
            write("{");
            write($"{Tab}vec4 Ambient;");
            write($"{Tab}vec4 Diffuse;");
            write($"{Tab}vec4 Specular;");
            write($"{Tab}vec4 Position;");
            write($"{Tab}vec4 Attenuation;");
            write($"{Tab}vec4 SpotDirection;");
            write($"{Tab}vec4 Half;");
            write("};");
            write("");

            write("struct LightModelStruct");
            write("{");
            write($"{Tab}vec4 Ambient;");
            write($"{Tab}vec4 SceneColor;");
            write("};");
            write("");

            write("struct LightProdStruct");
            write("{");
            write($"{Tab}vec4 Ambient;");
            write($"{Tab}vec4 Diffuse;");
            write($"{Tab}vec4 Specular;");
            write("};");
            write("");

            write("struct MaterialStruct");
            write("{");
            write($"{Tab}vec4 Ambient;");
            write($"{Tab}vec4 Diffuse;");
            write($"{Tab}vec4 Specular;");
            write($"{Tab}vec4 Emission;");
            write($"{Tab}vec4 Shininess;");
            write("};");
            write("");

            write("struct MatrixStruct");
            write("{");
            write($"{Tab}mat4 ModelView[MAX_VERTEX_UNITS_ARB];");
            write($"{Tab}mat4 Projection;");
            write($"{Tab}mat4 MVP;");
            write($"{Tab}mat4 Texture[MAX_TEXTURE_COORDS_ARB];");
            write($"{Tab}mat4 Palette[MAX_PALETTE_MATRICES_ARB];");
            write($"{Tab}mat4 Program[MAX_PROGRAM_MATRICES_ARB];");
            write("};");
            write("");

            write("struct PointStruct");
            write("{");
            write($"{Tab}vec4 Size;");
            write($"{Tab}vec4 Attenuation;");
            write("};");
            write("");

            write("struct TexGenStruct");
            write("{");
            write($"{Tab}vec4 EyeS;");
            write($"{Tab}vec4 EyeT;");
            write($"{Tab}vec4 EyeR;");
            write($"{Tab}vec4 EyeQ;");
            write($"{Tab}vec4 ObjectS;");
            write($"{Tab}vec4 ObjectT;");
            write($"{Tab}vec4 ObjectR;");
            write($"{Tab}vec4 ObjectQ;");
            write("};");
            write("");

            write("struct TexEnvStruct");
            write("{");
            write($"{Tab}vec4 Color;");
            write("};");
            write("");

            write("struct StateStruct");
            write("{");
            write($"{Tab}MaterialStruct Material[2];");
            write($"{Tab}LightStruct Light[MAX_LIGHTS];");
            write($"{Tab}LightModelStruct LightModel[2];");
            write($"{Tab}LightProdStruct LightProd[2][MAX_LIGHTS];");
            write($"{Tab}TexGenStruct TexGen[MAX_TEXTURE_UNITS_ARB];");
            write($"{Tab}TexEnvStruct TexEnv[MAX_TEXTURE_UNITS_ARB];");
            write($"{Tab}FogStruct Fog;");
            write($"{Tab}ClipStruct Clip[MAX_CLIP_PLANES];");
            write($"{Tab}PointStruct Point;");
            write($"{Tab}DepthStruct Depth;");
            write($"{Tab}MatrixStruct Matrix;");
            write($"{Tab}MatrixStruct MatrixInv;");
            write($"{Tab}MatrixStruct MatrixTrans;");
            write($"{Tab}MatrixStruct MatrixInvTrans;");
            write("};");
            write("");

            write("layout (binding = 0, std140) uniform StateUniform");
            write("{");
            write($"{Tab}StateStruct State;");
            write("};");
            write("");

            write("layout (binding = 1, std140) uniform EnvUniform");
            write("{");
            write($"{Tab}vec4 PrgFragEnv[MAX_PROGRAM_ENV_PARAMETERS_ARB];");
            write($"{Tab}vec4 PrgVertEnv[MAX_PROGRAM_ENV_PARAMETERS_ARB];");
            write("};");
            write("");

            for (int i = 0; i < MAX_PROGRAM_PARAMETER_BUFFER_BINDINGS_NV - 2; i++)
            {
                write($"layout (binding = {2 + i}, std140) uniform Buffer{i}Uniform");
                write("{");
                write($"{Tab}vec4 Buffer{i}[MAX_PROGRAM_PARAMETER_BUFFER_SIZE_NV / 16];");
                write("};");
                write("");
            }

            write("#define GetCC(a) ((a) > 0 ? 1 : (a) < 0 ? -1 : 0)");
            write("#define BCCEQ(a) ((a) == 0)");
            write("#define BCCGE(a) ((a) >= 0)");
            write("#define BCCGT(a) ((a) > 0)");
            write("#define BCCLE(a) ((a) <= 0)");
            write("#define BCCLT(a) ((a) < 0)");
            write("#define BCCNE(a) ((a) != 0)");
            write("#define BCCTR(a) (true)");
            write("#define BCCFL(a) (false)");
            write("");

            write("#define CCTR(a, b, c) (true ? b : c)");
            write("#define CCFL(a, b, c) (false ? b : c)");
            write("#define CCEQ(a, b, c) ((a) == 0 ? b : c)");
            write("#define CCGE(a, b, c) ((a) >= 0 ? b : c)");
            write("#define CCGT(a, b, c) ((a) > 0 ? b : c)");
            write("#define CCLE(a, b, c) ((a) <= 0 ? b : c)");
            write("#define CCLT(a, b, c) ((a) < 0 ? b : c)");
            write("#define CCNE(a, b, c) ((a) != 0 ? b : c)");
            write("#define CCTR(a, b, c) (true ? b : c)");
            write("#define CCFL(a, b, c) (false ? b : c)");
            write("");

            write("#define GetCCVec(a, b) mix(mix(vec##b(0), vec##b(-1), lessThan(a"
                + ", vec##b(0))), vec##b(1), greaterThan(a, vec##b(0)))");
            write("#define CCEQVec(a, b, c, d) mix(c, d, equal(a, vec##b(0)))");
            write("#define CCGEVec(a, b, c, d) mix(c, d, greaterThanEqual(a, vec##b(0)))");
            write("#define CCGTVec(a, b, c, d) mix(c, d, greaterThan(a, vec##b(0)))");
            write("#define CCLEVec(a, b, c, d) mix(c, d, lessThanEqual(a, vec##b(0)))");
            write("#define CCLTVec(a, b, c, d) mix(c, d, lessThan(a, vec##b(0)))");
            write("#define CCNEVec(a, b, c, d) mix(c, d, notEqual(a, vec##b(0)))");
            write("#define CCTRVec(a, b, c, d) mix(c, d, bvec##b(true))");
            write("#define CCFLVec(a, b, c, d) mix(c, d, bvec##b(false))");

            void write(string str)
            {
                byte[] buf = Encoding.GetBytes(str);
                s.Write(buf, 0, buf.Length);
                s.WriteByte((byte)'\n');
            }
        }

        public void ParseARBFile(string[] s, int i0)
        {
            int c, i, i1, i2;
            string ver = s[0];
                 if (ver.StartsWith("!!ARBvp") || ver.StartsWith("!!NVvp")
                || ver.StartsWith("!!VP")) mode = Mode.  VertexProgram;
            else if (ver.StartsWith("!!ARBfp") || ver.StartsWith("!!NVfp")
                || ver.StartsWith("!!FP")) mode = Mode.FragmentProgram;
            else throw new Exception($"0x003F Unsupported version {ver}");

            for (i = 0; i < i0; i++)
                if (s[i] == "END") break;

            if (i == i0) throw new Exception("0x0001 No END");

            Array.Resize(ref s, i);

            for (i0 = i, i = 1; i < i0; i++)
                if (!s[i].StartsWith("OPTION")) break;

            Array.Copy(s, i, s, 0, i0 -= i);
            Array.Resize(ref s, i0);

            attrib = new Dictionary<INameAttrib, IType>();
            output = new Dictionary<INameOutput, IType>();
            param  = new Dictionary<INameParam , IType>();
            inst = new List<IInstruction>();
            rename = new Dictionary<string, string>();
            renameAlt = new Dictionary<string, string>();
            temp = new List<Temp>();
            label = new List<string>();

            for (i = 0; i < i0; i++)
            {
                string str = s[i];
                i1 = 0;
                i2 = str.Length;
                while (i1 < i2 && str[i1] == ' ') i1++;
                str = str.Substring(i1);

                Sign sign;
                     if (str.StartsWith("+")) { sign = Sign.P; str = str.Substring(1); }
                else if (str.StartsWith("-")) { sign = Sign.M; str = str.Substring(1); }
                else                            sign = Sign.N;

                bool abs = str.Contains("|");
                if (abs) str = str.Replace("|", "");

                Modifier m = Modifier.FLOAT;
                     if (str.StartsWith("SHORT ")) { m = Modifier.SHORT; str = str.Substring(6); }
                else if (str.StartsWith( "LONG ")) { m = Modifier. LONG; str = str.Substring(5); }
                else if (str.StartsWith(  "INT ")) { m = Modifier.  INT; str = str.Substring(4); }
                else if (str.StartsWith( "UINT ")) { m = Modifier. UINT; str = str.Substring(5); }

                string a;
                string[] b;
                IType t;
                if (str.StartsWith("#"))
                    continue;

                else if (str.StartsWith("ATTRIB "))
                {
                    a = str.Substring(7).Replace(" ", "");
                    b = a.Split('=');
                    if (b.Length != 2) throw new Exception("0x0004 Invalid ATTRIB");

                    INameAttrib n;
                    if (b[0].Contains("["))
                    {
                        t = default;
                        i1 = b[0].IndexOf('[');

                        string d = b[0].Substring(i1 + 1).Replace("]", "");
                        string e = b[0].Substring(0, i1);
                        if (d.Contains(".."))
                        {
                            string[] g = d.Split(new string[] { ".." }, StringSplitOptions.RemoveEmptyEntries);
                            if (g.Length == 2 && int.TryParse(g[0], out int v0)
                                && int.TryParse(g[1], out int v1))
                                n = new NameAttribRange(m, e, v0, v1);
                            else throw new Exception("0x0005 ATTRIB: Not a range");
                        }
                        else if (int.TryParse(d, out int v))
                            n = new NameAttribIndex(m, b[0], v);
                        else throw new Exception("0x0006 ATTRIB: Not a number");
                    }
                    else n = new NameAttrib(m, b[0]);

                    t = GetSrcOperandType(mode, m, b[1], abs, sign);
                    renameAlt.Add(b[0], TypeToString(t, m));
                    attrib.Add(n, t);
                }
                else if (str.StartsWith("PARAM "))
                {
                    a = str.Substring(6).Replace(" ", "");
                    b = a.Split('=');
                    if (b.Length != 2) throw new Exception("0x0007 Invalid PARAM");

                    t = GetSrcOperandType(mode, m, b[1], abs, sign);
                    if (b[0].Contains("["))
                    {
                        i1 = b[0].IndexOf('[');
                        string d = b[0].Substring(i1 + 1);
                        d = d.Substring(0, d.IndexOf(']'));
                        string e = b[0].Substring(0, i1);

                        if (!(t is TypeArray ta)) ta = default;
                        if (!(t is TypeIndex ti)) ti = default;
                        if (!(t is TypeRange tr)) tr = default;
                        if (int.TryParse(d, out int v)) { }
                        else if (!b[0].EndsWith("[]")) throw new Exception("0x0008 PARAM: Not a number");
                        else if (t is TypeRange) v = tr.IDEnd - tr.IDStart + 1;
                        else if (t is TypeArray && ta.Array != null) v = ta.Array.Length;
                        else throw new Exception("0x0000 unk");

                        string tmp = "{0}";
                        string tmp2 = $"{e}[{tmp}]";
                        string tmp3 = "State.Matrix";
                        switch (t.Var)
                        {
                            case Var.Array:
                                param.Add(new NameParamRange(m, e, 0, v - 1), t);
                                continue;
                            //case Var.FragmentInputClipNO:
                            case Var.FragmentProgramEnvNO         :
                                tmp3 = $"PrgFragEnv";   if (tr.IDStart > 0) tmp = $"{tmp} + {tr.IDStart}"; break;
                            case Var.FragmentProgramLocalNO       :
                                tmp3 = $"PrgFragLocal"; if (tr.IDStart > 0) tmp = $"{tmp} + {tr.IDStart}"; break;
                            case Var.VertexProgramEnvNO           :
                                tmp3 = $"PrgVertEnv";   if (tr.IDStart > 0) tmp = $"{tmp} + {tr.IDStart}"; break;
                            case Var.VertexProgramLocalNO         :
                                tmp3 = $"PrgVertLocal"; if (tr.IDStart > 0) tmp = $"{tmp} + {tr.IDStart}"; break;
                            case Var.StateMatrixModelViewN        :
                                tmp3 += $".ModelView[{ti.ID}]";         break;
                            case Var.StateMatrixMVP               :
                                tmp3 += $".MVP";                        break;
                            case Var.StateMatrixPaletteN          :
                                tmp3 += $".Palette[{ti.ID}]";           break;
                            case Var.StateMatrixProgramN          :
                                tmp3 += $".Program[{ti.ID}]";           break;
                            case Var.StateMatrixProjection        :
                                tmp3 += $".Projection";                 break;
                            case Var.StateMatrixTextureN          :
                                tmp3 += $".Texture[{ti.ID}]";           break;
                            case Var.StateMatrixInvModelViewN     :
                                tmp3 += $"Inv.ModelView[{ti.ID}]";      break;
                            case Var.StateMatrixInvMVP            :
                                tmp3 += $"Inv.MVP";                     break;
                            case Var.StateMatrixInvPaletteN       :
                                tmp3 += $"Inv.Palette[{ti.ID}]";        break;
                            case Var.StateMatrixInvProgramN       :
                                tmp3 += $"Inv.Program[{ti.ID}]";        break;
                            case Var.StateMatrixInvProjection     :
                                tmp3 += $"Inv.Projection";              break;
                            case Var.StateMatrixInvTextureN       :
                                tmp3 += $"Inv.Texture[{ti.ID}]";        break;
                            case Var.StateMatrixTransModelViewN   :
                                tmp3 += $"Trans.ModelView[{ti.ID}]";    break;
                            case Var.StateMatrixTransMVP          :
                                tmp3 += $"Trans.MVP";                   break;
                            case Var.StateMatrixTransPaletteN     :
                                tmp3 += $"Trans.Palette[{ti.ID}]";      break;
                            case Var.StateMatrixTransProgramN     :
                                tmp3 += $"Trans.Program[{ti.ID}]";      break;
                            case Var.StateMatrixTransProjection   :
                                tmp3 += $"Trans.Projection";            break;
                            case Var.StateMatrixTransTextureN     :
                                tmp3 += $"Trans.Texture[{ti.ID}]";      break;
                            case Var.StateMatrixInvTransModelViewN:
                                tmp3 += $"InvTrans.ModelView[{ti.ID}]"; break;
                            case Var.StateMatrixInvTransMVP       :
                                tmp3 += $"InvTrans.MVP";                break;
                            case Var.StateMatrixInvTransPaletteN  :
                                tmp3 += $"InvTrans.Palette[{ti.ID}]";   break;
                            case Var.StateMatrixInvTransProgramN  :
                                tmp3 += $"InvTrans.Program[{ti.ID}]";   break;
                            case Var.StateMatrixInvTransProjection:
                                tmp3 += $"InvTrans.Projection";         break;
                            case Var.StateMatrixInvTransTextureN  :
                                tmp3 += $"InvTrans.Texture[{ti.ID}]";   break;
                            default:
                                throw new Exception($"0x0044 PARAM: Invalid Type {t.Var}");
                        }
                        tmp3 += $"[{tmp}]";
                        renameAlt.Add(string.Format(tmp2, "N"), string.Format(tmp3, "N"));
                        rename.Add(tmp2, tmp3);
                        for (i1 = 0; i1 < v; i1++)
                            rename.Add(string.Format(tmp2, i1), string.Format(tmp3, i1));
                    }
                    else if (t is Type || t is TypeIndex || t is TypeIndexIndex || t is TypeIndexName)
                    {
                        string tmp = TypeToString(t, m);
                        renameAlt.Add(b[0], tmp);
                        rename.Add(b[0], tmp);
                    }
                    else if (!(t is TypeRange))
                        param.Add(new NameParam(m, b[0]), t);
                    else
                        throw new Exception("0x001B unk");
                }
                else if (str.StartsWith("OUTPUT "))
                {
                    a = str.Substring(7).Replace(" ", "");
                    b = a.Split('=');
                    if (b.Length != 2) throw new Exception("0x0009 Invalid OUTPUT");

                    INameOutput n;
                    if (b[0].Contains("["))
                    {
                        t = default;
                        i1 = b[0].IndexOf('[');

                        string d = b[0].Substring(i1 + 1).Replace("]", "");
                        string e = b[0].Substring(0, i1);
                        if (d.Contains(".."))
                        {
                            string[] g = d.Split(new string[] { ".." }, StringSplitOptions.RemoveEmptyEntries);
                            if (g.Length == 2 && int.TryParse(g[0], out int v0)
                                && int.TryParse(g[1], out int v1))
                                n = new NameOutputRange(m, e, v0, v1);
                            else throw new Exception("0x000A OUTPUT: Not a range");
                        }
                        else if (int.TryParse(d, out int v))
                            n = new NameOutputIndex(m, b[0], v);
                        else throw new Exception("0x000B OUTPUT: Not a number");
                    }
                    else n = new NameOutput(m, b[0]);

                    t = GetSrcOperandType(mode, m, b[1], abs, sign);
                    renameAlt.Add(b[0], TypeToString(t, m));
                    output.Add(n, t);
                }
                else if (str.StartsWith("ADDRESS "))
                {
                    a = str.Substring(8).Replace(" ", "");
                    b = a.Split(',');
                    c = b.Length;
                    for (i1 = 0; i1 < c; i1++)
                        temp.Add(new Temp(Modifier.INT, b[i1]));
                }
                else if (str.StartsWith("TEMP "))
                {
                    a = str.Substring(5).Replace(" ", "");
                    b = a.Split(',');
                    c = b.Length;
                    for (i1 = 0; i1 < c; i1++)
                        temp.Add(new Temp(m, b[i1]));
                }
                else if (str.StartsWith("BUFFER "))
                {
                    a = str.Substring(7).Replace(" ", "");
                    b = a.Split('=');
                    if (b.Length != 2) throw new Exception("0x0074 Invalid BUFFER");
                    goto Buffer;
                }
                else if (str.StartsWith("CBUFFER "))
                {
                    a = str.Substring(8).Replace(" ", "");
                    b = a.Split('=');
                    if (b.Length != 2) throw new Exception("0x0075 Invalid CBUFFER");
                    goto Buffer;
                }
                else if (str.StartsWith("BUFFER4 "))
                {
                    a = str.Substring(8).Replace(" ", "");
                    b = a.Split('=');
                    if (b.Length != 2) throw new Exception("0x0076 Invalid BUFFER4");
                    goto Buffer;
                }
                else break;
                continue;

            Buffer:
                {
                    t = GetSrcOperandType(mode, m, b[1], abs, sign);
                    if (b[0].Contains("["))
                    {
                        i1 = b[0].IndexOf('[');
                        string d = b[0].Substring(i1 + 1);
                        d = d.Substring(0, d.IndexOf(']'));
                        string e = b[0].Substring(0, i1);

                        if (!(t is TypeIndex ti)) ti = default;
                        if (!(t is TypeIndexRange tir)) tir = default;

                        if (int.TryParse(d, out int v)) { }
                        else if (b[0].EndsWith("[]"))
                        {
                                 if (t is TypeIndex) v = MAX_PROGRAM_PARAMETER_BUFFER_SIZE_NV;
                            else if (t is TypeIndexRange) v = tir.IDEnd - tir.IDStart + 1;
                        }
                        else throw new Exception("0x0077: Not a number");

                        string tmp = "{0}";
                        string tmp2 = $"{e}[{tmp}]";
                        string tmp3 = "State.Matrix";
                        switch (t.Var)
                        {
                            case Var.ProgramBufferN:
                                tmp3 = $"Buffer{ti.ID}"; break;
                            case Var.ProgramBufferNOP:
                                tmp3 = $"Buffer{tir.ID}"; if (tir.IDStart > 0) tmp = $"{tmp} + {tir.IDStart}"; break;
                            default:
                                throw new Exception($"0x0078 Invalid Type {t.Var}");
                        }
                        tmp3 += $"[{tmp}]";
                        renameAlt.Add(string.Format(tmp2, "N"), string.Format(tmp3, "N"));
                        rename.Add(tmp2, tmp3);
                        for (i1 = 0; i1 < v; i1++)
                            rename.Add(string.Format(tmp2, i1), string.Format(tmp3, i1));
                    }
                    else if (t is TypeIndexIndex tii)
                    {
                        string tmp = TypeToString(t, m);
                        renameAlt.Add(b[0], tmp);
                        rename.Add(b[0], tmp);
                    }
                    else
                        throw new Exception("0x0079 unk");
                }
            }
            Array.Copy(s, i, s, 0, i0 -= i);
            Array.Resize(ref s, i0);

            hasCC = false;
            c = 0;
            for (i = 0; i < i0; i++)
            {
                string str = s[i];
                i1 = 0;
                i2 = str.Length;
                while (i1 < i2 && str[i1] == ' ') i1++;
                str = str.Substring(i1);
                if (str == "") continue;

                string[] g = str.Split(':');
                for (int i3 = 0; i3 < g.Length - 1; i3++)
                {
                    str = g[i3].Replace(" ", "");
                    if (str == "") continue;

                    inst.Add(new Label(str));
                    c++;
                }
                str = g[g.Length - 1];

                i1 = 0;
                i2 = str.Length;
                while (i1 < i2 && str[i1] == ' ') i1++;
                str = str.Substring(i1);
                if (str == "") continue;

                if (str.StartsWith("ALIAS "))
                {
                    string a = str.Substring(6).Replace(" ", "");
                    string[] b = a.Split('=');
                    renameAlt.Add(b[0], b[1]);
                    rename.Add(b[0], b[1]);
                    continue;
                }

                string d;
                g = null;
                if (str.Contains(" "))
                {
                    i1 = 0;
                    while (str[i1] != ' ') i1++;
                    i2 = i1;
                    while (str[i2] == ' ') i2++;

                    d = str.Substring(0, i1);
                    string e = str.Substring(i2).Replace(", ", ",").Replace(" ", "");
                    if (!e.Contains("{") && !e.Contains(",(")) g = e.Split(',');
                    else
                    {
                        string h;
                        if (e.Contains("{"))
                            i2 = e.IndexOf("{");
                        else
                            i2 = e.IndexOf("(");

                        h = e.Substring(i2);
                        g = e.Substring(0, i2).Split(',');
                        g[g.Length - 1] = h;
                        if (e.Contains(",{") || e.Contains(",("))
                        {
                            h = h.Replace(",{", "\x01{").Replace("},", "}\x01");
                            h = h.Replace(",(", "\x01(").Replace("),", ")\x01");
                            string[] g0 = h.Split('\x01');
                            int s1 = g.Length - 1;
                            int s2 = g0.Length;
                            Array.Resize(ref g, s1 + s2);
                            for (i2 = 0; i2 < s2; i2++)
                                g[s1 + i2] = g0[i2];

                            for (i2 = 0; i2 < s2; i2++)
                            {
                                if (g[s1 + i2].Contains("{") || g[s1 + i2].Contains("(") || !g[s1 + i2].Contains(","))
                                    continue;

                                g0 = g[s1 + i2].Split(',');
                                s1 += i2;
                                int s3 = g0.Length;
                                Array.Resize(ref g, s1 + s3);
                                for (int i3 = 0; i3 < s3; i3++)
                                    g[s1 + i3] = g0[i3];

                                s1 += s3 - 1;
                                s2 -= s3;
                                i2 = 0;
                            }
                        }
                    }
                }
                else { d = str; g = null; }

                Modifier m = Modifier.FLOAT; InstFlags flags = InstFlags.N;
                     if (d.EndsWith(".CC" )) { flags |= InstFlags.CC ; d = d.Substring(0, d.Length - 3); hasCC |= true; }
                else if (d.EndsWith(".CC0")) { flags |= InstFlags.CC0; d = d.Substring(0, d.Length - 4); hasCC |= true; }
                else if (d.EndsWith(".CC1")) { flags |= InstFlags.CC1; d = d.Substring(0, d.Length - 4); hasCC |= true; }

                     if (d.EndsWith(".F")) { flags |= InstFlags.F; d = d.Substring(0, d.Length - 2); }
                else if (d.EndsWith(".S")) { flags |= InstFlags.S; d = d.Substring(0, d.Length - 2); }
                else if (d.EndsWith(".U")) { flags |= InstFlags.U; d = d.Substring(0, d.Length - 2); }

                     if (d.EndsWith("_SAT" )) { flags |= InstFlags.S; d = d.Substring(0, d.Length - 4); }
                else if (d.EndsWith("_SSAT")) { flags |= InstFlags.s; d = d.Substring(0, d.Length - 5); }

                i1 = d.Length;
                if (i1 >= 4 && d.EndsWith("C"))
                {
                    if ((flags & (InstFlags.CC | InstFlags.CC0 | InstFlags.CC1)) != 0)
                        throw new Exception("0x006C CC already exist for this instruction");
                    flags |= InstFlags.C; d = d.Substring(0, d.Length - 1);
                    hasCC |= true;
                }
                if (i1 >= 5 && d.EndsWith("C0"))
                {
                    if ((flags & (InstFlags.CC | InstFlags.CC0 | InstFlags.CC1)) != 0)
                        throw new Exception("0x009A CC already exist for this instruction");
                    flags |= InstFlags.CC0; d = d.Substring(0, d.Length - 2);
                    hasCC |= true;
                }
                if (i1 >= 5 && d.EndsWith("C1"))
                {
                    if ((flags & (InstFlags.CC | InstFlags.CC0 | InstFlags.CC1)) != 0)
                        throw new Exception("0x009B CC already exist for this instruction");
                    flags |= InstFlags.CC1; d = d.Substring(0, d.Length - 2);
                    hasCC |= true;
                }

                i1 = d.Length;
                     if (i1 > 3 && d.EndsWith("R")) { flags |= InstFlags.R; d = d.Substring(0, d.Length - 1); }
                else if (i1 > 3 && d.EndsWith("H")) { flags |= InstFlags.H; d = d.Substring(0, d.Length - 1); }
                else if (i1 > 3 && d.EndsWith("X")) { flags |= InstFlags.X; d = d.Substring(0, d.Length - 1); }

                i1 = g == null ? 0 : g.Length;
                IInstruction ins = d switch
                {
                    "CVT.S32.F32" => new ARL(this, g, mode, m, flags), // PDAFT uses this only as ARL
                    "ABS"   => new ABS  (this, g, mode, m, flags),
                    "ADD"   => new ADD  (this, g, mode, m, flags),
                    "ARL"   => new ARL  (this, g, mode, m, flags),
                    "ARR"   => new ARR  (this, g, mode, m, flags),
                    "CEIL"  => new CEIL (this, g, mode, m, flags),
                    "CMP"   => new CMP  (this, g, mode, m, flags),
                    "COS"   => new COS  (this, g, mode, m, flags),
                    "DDX"   => new DDX  (this, g, mode, m, flags),
                    "DDY"   => new DDY  (this, g, mode, m, flags),
                    "DIV"   => new DIV  (this, g, mode, m, flags),
                    "DP2"   => new DP2  (this, g, mode, m, flags),
                    "DP2A"  => new DP2A (this, g, mode, m, flags),
                    "DP3"   => new DP3  (this, g, mode, m, flags),
                    "DP4"   => new DP4  (this, g, mode, m, flags),
                    "DPH"   => new DPH  (this, g, mode, m, flags),
                    "DST"   => new DST  (this, g, mode, m, flags),
                    "EX2"   => new EX2  (this, g, mode, m, flags),
                    "EXP"   => new EXP  (this, g, mode, m, flags),
                    "FLR"   => new FLR  (this, g, mode, m, flags),
                    "FRC"   => new FRC  (this, g, mode, m, flags),
                    "LG2"   => new LG2  (this, g, mode, m, flags),
                    "LIT"   => new LIT  (this, g, mode, m, flags),
                    "LOG"   => new LOG  (this, g, mode, m, flags),
                    "LRP"   => new LRP  (this, g, mode, m, flags),
                    "MAD"   => new MAD  (this, g, mode, m, flags),
                    "MAX"   => new MAX  (this, g, mode, m, flags),
                    "MIN"   => new MIN  (this, g, mode, m, flags),
                    "MOV"   => new MOV  (this, g, mode, m, flags),
                    "MUL"   => new MUL  (this, g, mode, m, flags),
                    "NRM"   => new NRM  (this, g, mode, m, flags),
                    "POW"   => new POW  (this, g, mode, m, flags),
                    "RCC"   => new RCC  (this, g, mode, m, flags),
                    "RCP"   => new RCP  (this, g, mode, m, flags),
                    "RFL"   => new RFL  (this, g, mode, m, flags),
                    "RSQ"   => new RSQ  (this, g, mode, m, flags),
                    "SCS"   => new SCS  (this, g, mode, m, flags),
                    "SEQ"   => new SEQ  (this, g, mode, m, flags),
                    "SFL"   => new SFL  (this, g, mode, m, flags),
                    "SGE"   => new SGE  (this, g, mode, m, flags),
                    "SGT"   => new SGT  (this, g, mode, m, flags),
                    "SIN"   => new SIN  (this, g, mode, m, flags),
                    "SLE"   => new SLE  (this, g, mode, m, flags),
                    "SLT"   => new SLT  (this, g, mode, m, flags),
                    "SNE"   => new SNE  (this, g, mode, m, flags),
                    "SSG"   => new SSG  (this, g, mode, m, flags),
                    "STR"   => new STR  (this, g, mode, m, flags),
                    "SUB"   => new SUB  (this, g, mode, m, flags),
                    "SWZ"   => new SWZ  (this, g, mode, m, flags),
                    "TEX"   => new TEX  (this, g, mode, m, flags),
                    "TRUN"  => new TRUNC(this, g, mode, m, flags & ~InstFlags.C),
                    "TRUNC" => new TRUNC(this, g, mode, m, flags),
                    "TXB"   => new TXB  (this, g, mode, m, flags),
                    "TXL"   => new TXL  (this, g, mode, m, flags),
                    "TXP"   => new TXP  (this, g, mode, m, flags),
                    "X2D"   => new X2D  (this, g, mode, m, flags),
                    "XPD"   => new XPD  (this, g, mode, m, flags),

                    "BRA"     => new BRA    (g, label),
                    //"BRK"     => default,
                    "CAL"     => new CAL    (g, label),
                    //"CONT"    => default,
                    "ELSE"    => new ELSE   (),
                    "ENDIF"   => new ENDIF  (),
                    "ENDLOOP" => new ENDLOOP(),
                    "ENDREP"  => new ENDREP (),
                    "IF"      => new IF     (g),
                    "KIL"     => new KIL    (this, g, mode, m, flags),
                    "LOOP"    => new LOOP   (this, g, mode, m, flags),
                    "REP"     => new REP    (this, g, mode, m, flags),
                    "RET"     => new RET    (this, g, mode, m, flags),
                    _ when i1 == 0 => new Dummy0(this, g, mode, m, flags, d),
                    _ when i1 == 1 => new Dummy1(this, g, mode, m, flags, d),
                    _ when i1 == 2 => new Dummy2(this, g, mode, m, flags, d),
                    _ when i1 == 3 => new Dummy3(this, g, mode, m, flags, d),
                    _ when i1 == 4 => new Dummy4(this, g, mode, m, flags, d),
                    _ when i1 == 5 => new Dummy5(this, g, mode, m, flags, d),
                    _ => throw new Exception($"0x0046 Unsupported instruction {s[i]}"),
                };
                inst.Add(ins);
                c++;
            }
        }

        private void WriteProgram()
        {
            int i, i0, i1;

            Dictionary<INameAttrib, string>     @in      = new Dictionary<INameAttrib, string>();
            Dictionary<INameOutput, string>    @out      = new Dictionary<INameOutput, string>();
            Dictionary<INameParam , string>  @const      = new Dictionary<INameParam , string>();
            Dictionary<INameParam , string>   constArray = new Dictionary<INameParam , string>();

            {
                i0 = attrib.Count;
                INameAttrib[] attribArr = GetDictKeys(attrib);
                for (i = 0; i < i0; i++)
                {
                    if (@in.ContainsKey(attribArr[i]))
                        throw new Exception($"0x0049 {attribArr[i]} is already present");

                    @in.Add(attribArr[i], TypeToString(attrib[attribArr[i]], Modifier.FLOAT));
                }
                attribArr = null;
            }

            {
                i0 = output.Count;
                INameOutput[] outputArr = GetDictKeys(output);
                for (i = 0; i < i0; i++)
                {
                    if (@out.ContainsKey(outputArr[i]))
                        throw new Exception($"0x004A {outputArr[i]} is already present");

                    @out.Add(outputArr[i], TypeToString(output[outputArr[i]], Modifier.FLOAT));
                }
                outputArr = null;
            }

            {
                i0 = param.Count;
                INameParam[] paramArr = GetDictKeys(param);
                for (i = 0; i < i0; i++)
                {
                    IType t = param[paramArr[i]];
                    if (t is Type || t is TypeIndex || t is TypeIndexIndex || t is TypeIndexName || t is TypeRange) { }
                    else if (t is TypeName) throw new Exception($"AAAAAAA");
                    else if (t is TypeArray ta)
                    {
                        string type = getType(paramArr[i]);
                        int c = ta.Array.Length;
                        string val = $"{c}{"\x01{\n"}";
                        for (i1 = 0; i1 < c; i1++)
                            val += $"{Tab}{type}{getString(ta.Array[i1])}{(i1 + 1 < c ? ",\n" : "\n")}";
                        val += "}";
                        constArray.Add(paramArr[i], val);
                    }
                    else addConst(getString(t));

                    static string getString(IType t)
                    {
                        if (t.Var == Var.Value)
                            switch (t)
                            {
                                case TypeVec<float> tvf: return $"({tvf.X}, {0}, {0}, {1})";
                                case TypeVec<  int> tvi: return $"({tvi.X}, {0}, {0}, {1})";
                                case TypeVec< uint> tvu: return $"({tvu.X}, {0}, {0}, {1})";
                                default: throw new Exception("0x000D Invalid TypeValue<>");
                            }
                        else if (t.Var == Var.Vector2)
                            switch (t)
                            {
                                case TypeVec<float> tvf: return $"({tvf.X}, {tvf.Y}, {0}, {1})";
                                case TypeVec<  int> tvi: return $"({tvi.X}, {tvi.Y}, {0}, {1})";
                                case TypeVec< uint> tvu: return $"({tvu.X}, {tvu.Y}, {0}, {1})";
                                default: throw new Exception("0x000E Invalid TypeVec2<>");
                            }
                        else if (t.Var == Var.Vector3)
                            switch (t)
                            {
                                case TypeVec<float> tvf: return $"({tvf.X}, {tvf.Y}, {tvf.Z}, {1})";
                                case TypeVec<  int> tvi: return $"({tvi.X}, {tvi.Y}, {tvi.Z}, {1})";
                                case TypeVec< uint> tvu: return $"({tvu.X}, {tvu.Y}, {tvu.Z}, {1})";
                                default: throw new Exception("0x000F Invalid TypeVec3<>");
                            }
                        else if (t.Var == Var.Vector4)
                            switch (t)
                            {
                                case TypeVec<float> tvf: return $"({tvf.X}, {tvf.Y}, {tvf.Z}, {tvf.W})";
                                case TypeVec<  int> tvi: return $"({tvi.X}, {tvi.Y}, {tvi.Z}, {tvi.W})";
                                case TypeVec< uint> tvu: return $"({tvu.X}, {tvu.Y}, {tvu.Z}, {tvu.W})";
                                default: throw new Exception("0x0010 Invalid TypeVec4<>");
                            }
                        else throw new Exception("0x0011 Invalid IType");
                    }

                    void addConst(string str)
                    { if (!@const.ContainsKey(paramArr[i])) @const.Add(paramArr[i], str); }
                }
                paramArr = null;
            }

            match = new Dictionary<string, string>();
            matchMod = new Dictionary<string, Modifier>();
            write("#version 430 core");
            WriteUniform();

            {
                i0 = @in.Count;
                INameAttrib[] inArr = GetDictKeys(@in);
                for (i = 0; i < i0; i++)
                {
                    string str = @in[inArr[i]];
                    addStrDict(inArr[i].Var, str, inArr[i].Mod);
                }
                inArr = null;
            }

            {
                i0 = @out.Count;
                INameOutput[] outArr = GetDictKeys(@out);
                for (i = 0; i < i0; i++)
                {
                    string str = @out[outArr[i]];
                    addStrDict(outArr[i].Var, str, outArr[i].Mod);
                }
                outArr = null;
            }

            {
                i0 = @const.Count;
                INameParam[] constArr = GetDictKeys(@const);
                List<string> constList = new List<string>();
                for (i = 0; i < i0; i++)
                {
                    string vec = @const[constArr[i]];
                    if (constList.Contains(constArr[i].Var)) continue;

                    addStrDict(constArr[i].Var, constArr[i].Var, constArr[i].Mod);
                    constList.Add(constArr[i].Var);
                    write($"const {getTypeString(constArr[i])} = {getType(constArr[i])}{vec};");
                }
                if (i0 > 0) write("");
                constList = null;
                constArr = null;
            }

            {
                i0 = constArray.Count;
                INameParam[] constArrayArr = GetDictKeys(constArray);
                List<string> constArrayList = new List<string>();
                for (i = 0; i < i0; i++)
                {
                    string[] vec = constArray[constArrayArr[i]].Split('\x01');
                    if (constArrayList.Contains(constArrayArr[i].Var)) continue;

                    if (!(constArrayArr[i] is NameParamRange npr))
                        throw new Exception($"0x0065 Invalid");

                    int v = npr.IDEnd - npr.IDStart + 1;
                    string s = constArrayArr[i].Var;

                    for (i1 = 0; i1 < v; i1++)
                        addStrDict($"{s}[{i1}]", $"{s}[{i1}]", constArrayArr[i].Mod);
                    constArrayList.Add(s);
                    write($"const {getTypeString(constArrayArr[i])}[{vec[0]}] = {vec[1]};");
                }
                if (i0 > 0) write("");
                constArrayList = null;
                constArrayArr = null;
            }

            {
                List<string> tempList = new List<string>();
                i0 = temp.Count;
                for (i = 0; i < i0; i++)
                {
                    string str = temp[i].Var;
                    if (tempList.Contains(str)) continue;

                    if (str == "half")
                    {
                        addStrDict(str, $"_{str}", temp[i].Mod);
                        write($"{getTypeMod(temp[i].Mod)} _{str}; // Renamed \"{str}\" to \"_{str}\"");
                    }
                    else
                    {
                        addStrDict(str, str, temp[i].Mod);
                        write($"{getTypeMod(temp[i].Mod)} {str};");
                    }
                    tempList.Add(str);
                }
                if (i0 > 0) write("");
                tempList = null;
            }

            {
                i0 = rename.Count;
                string[] renameKeyArr = GetDictKeys  (rename);
                string[] renameValArr = GetDictValues(rename);
                for (i = 0; i < i0; i++)
                    addStrDict(renameKeyArr[i], renameValArr[i], Modifier.FLOAT);
            }

            /*
            write("uniform sampler1D Texture1D;");
            write("uniform sampler2D Texture2D;");
            write("uniform sampler3D Texture3D;");
            write("uniform samplerCube TextureCUBE;");
            write("uniform sampler2DRect TextureRECT;");
            write("uniform sampler1DShadow TextureSHADOW1D;");
            write("uniform sampler2DShadow TextureSHADOW2D;");
            write("uniform sampler2DRectShadow TextureSHADOWRECT;");
            write("");*/

            SortedDictionary<int, string> texDict = new SortedDictionary<int, string>();
            i0 = inst.Count;
            for (i = 0; i < i0; i++)
            {
                string name;
                int index;
                switch (inst[i])
                {
                    case TEX tex:
                        name = tex.Mode switch
                        {
                            TexMode._1D => "1D",
                            TexMode._2D => "2D",
                            TexMode._3D => "3D",
                            TexMode._CUBE => "CUBE",
                            TexMode._RECT => "RECT",
                            TexMode._SHADOW1D => "SHADOW1D",
                            TexMode._SHADOW2D => "SHADOW2D",
                            TexMode._SHADOWRECT => "SHADOWRECT",
                            _ => throw new Exception("0x009F Invalid texture mode"),
                        };
                        index = tex.Index;
                        break;
                    case TXB txb:
                        name = txb.Mode switch
                        {
                            TexMode._1D => "1D",
                            TexMode._2D => "2D",
                            TexMode._3D => "3D",
                            TexMode._CUBE => "CUBE",
                            TexMode._RECT => "RECT",
                            TexMode._SHADOW1D => "SHADOW1D",
                            TexMode._SHADOW2D => "SHADOW2D",
                            TexMode._SHADOWRECT => "SHADOWRECT",
                            _ => throw new Exception("0x00A0 Invalid texture mode"),
                        };
                        index = txb.Index;
                        break;
                    case TXL txl:
                        name = txl.Mode switch
                        {
                            TexMode._1D => "1D",
                            TexMode._2D => "2D",
                            TexMode._3D => "3D",
                            TexMode._CUBE => "CUBE",
                            TexMode._SHADOW1D => "SHADOW1D",
                            TexMode._SHADOW2D => "SHADOW2D",
                            _ => throw new Exception("0x00A1 Invalid texture mode"),
                        };
                        index = txl.Index;
                        break;
                    case TXP txp:
                        name = txp.Mode switch
                        {
                            TexMode._1D => "1D",
                            TexMode._2D => "2D",
                            TexMode._3D => "3D",
                            TexMode._CUBE => "CUBE",
                            TexMode._RECT => "RECT",
                            TexMode._SHADOW1D => "SHADOW1D",
                            TexMode._SHADOW2D => "SHADOW2D",
                            TexMode._SHADOWRECT => "SHADOWRECT",
                            _ => throw new Exception("0x00A2 Invalid texture mode"),
                        };
                        index = txp.Index;
                        break;
                    default: continue;
                }

                if (texDict.ContainsKey(index))
                {
                    if (texDict[index] == $"Texture{name}{index}") continue;
                    else throw new Exception("0x00A3 Invalid");
                }

                texDict.Add(index, $"Texture{name}{index}");
            }
            
            i0 = texDict.Count;
            if (i0 > 0)
            {
                for (i = 0; i < MAX_TEXTURE_IMAGE_UNITS_ARB; i++)
                {
                    if (!texDict.ContainsKey(i))
                        continue;

                    string name = texDict[i];
                    string type;
                         if (name.StartsWith("Texture1D")) type = "sampler1D";
                    else if (name.StartsWith("Texture2D")) type = "sampler2D";
                    else if (name.StartsWith("Texture3D")) type = "sampler3D";
                    else if (name.StartsWith("TextureCUBE")) type = "samplerCube";
                    else if (name.StartsWith("TextureRECT")) type = "sampler2DRect";
                    else if (name.StartsWith("TextureSHADOW1D")) type = "sampler1DShadow";
                    else if (name.StartsWith("TextureSHADOW2D")) type = "sampler2DShadow";
                    else if (name.StartsWith("TextureSHADOWRECT")) type = "sampler2DRectShadow";
                    else throw new Exception("0x00A4 Invalid");
                    write($"layout(location = {i}) uniform {type} {name};");
                }
                write("");
            }

            if (hasCC)
            {
                write("vec4 cc0, cc1;");
                write("");
            }

            {
                i0 = renameAlt.Count;
                string[] renameAltKeyArr = GetDictKeys  (renameAlt);
                string[] renameAltValArr = GetDictValues(renameAlt);
                for (i = 0; i < i0; i++)
                    write($"// Renamed \"{renameAltKeyArr[i]}\" to \"{renameAltValArr[i]}\"");
                if (i0 > 0) write("");
            }

            i = 0;
            i0 = inst.Count;
            i1 = 0;
            instLevel = 0;
            write("void main()");
            write("{");
            if (mode == Mode.FragmentProgram)
                write($"{Tab}vec4 frontFacing = vec4(gl_FrontFacing ? 1.0 : -1.0, 0.0, 0.0, 1.0);");
            List<string> unkInsts = WriteInsts(Tab, ref i, ref i0, ref i1);
            write("}");

            if (unkInsts.Count > 0)
                throw new Exception($"0x0066 Has unknown instruction(s): {string.Join(", ", unkInsts)}");

            void write(string str)
            {
                byte[] buf = Encoding.GetBytes(str);
                s.Write(buf, 0, buf.Length);
                s.WriteByte((byte)'\n');
            }

            void addStrDict(string key, string val, Modifier m)
            {
                if (match.ContainsKey(key)) return;
                match.Add(key, val);
                match.Add($"+{key}", $"+{val}");
                match.Add($"-{key}", $"-{val}");
                matchMod.Add(key, m);
                matchMod.Add($"+{key}", m);
                matchMod.Add($"-{key}", m);
            }

            static string getType(IName no) =>
                no.Mod switch
                {
                    Modifier. INT => "ivec4",
                    Modifier.UINT => "uvec4",
                    _ => "vec4"
                };

            static string getTypeMod(Modifier m) =>
                m switch
                {
                    Modifier. INT => "ivec4",
                    Modifier.UINT => "uvec4",
                    _ => "vec4"
                };

            static string getTypeString(IName no) =>
                no.Mod switch
                {
                    Modifier. INT => "ivec4",
                    Modifier.UINT => "uvec4",
                    _ => "vec4"
                } + " " + no.Var;

            static T1[] GetDictKeys<T1, T2>(Dictionary<T1, T2> dict)
            {
                Dictionary<T1, T2>.KeyCollection kc = dict.Keys;
                T1[] arr = new T1[kc.Count];
                kc.CopyTo(arr, 0);
                return arr;
            }

            static T2[] GetDictValues<T1, T2>(Dictionary<T1, T2> dict)
            {
                Dictionary<T1, T2>.ValueCollection vc = dict.Values;
                T2[] arr = new T2[vc.Count];
                vc.CopyTo(arr, 0);
                return arr;
            }
        }

        private void WriteUniform()
        {
            if (useInclude)
            {
                string str = "";
                     if (mode == Mode.  VertexProgram) str = "Vert";
                else if (mode == Mode.FragmentProgram) str = "Frag";

                write($"#include \"{$"shared{str}.glsl"}\"");
                write($"#include \"{$"shared.glsl"}\"");
            }
            else
            {
                     if (mode == Mode.  VertexProgram) WriteSharedVertGLSL();
                else if (mode == Mode.FragmentProgram) WriteSharedFragGLSL();
                write("");

                WriteSharedGLSL();
                write("");
            }

            void write(string str)
            {
                byte[] buf = Encoding.GetBytes(str);
                s.Write(buf, 0, buf.Length);
                s.WriteByte((byte)'\n');
            }
        }

        private List<string> WriteInsts(string tab, ref int i, ref int i0, ref int i1, bool call = false)
        {
            List<string> unkInsts = new List<string>();
            for (; i < i0; i++)
            {
                WriteInst(inst[i], tab, ref i, ref i0, ref i1,
                    out bool @return, out bool hasReturn, out string failInstName, call);

                if (failInstName != null && !unkInsts.Contains(failInstName))
                    unkInsts.Add(failInstName);

                if (@return) break;
            }
            unkInsts.Sort();
            return unkInsts;
        }

        private void WriteInst(IInstruction inst, string tab, ref int i, ref int i0, ref int tmpNum,
            out bool @return, out bool hasReturn, out string failInstName, bool call = false)
        {
            @return = false;
            hasReturn = false;
            failInstName = null;

            int i1 = tmpNum;
            int i2;
            IType dt, st, s1t, s2t, s3t;
            string dn, sn, s1n, s2n, s3n;
            string dms, ssw, s1sw, s2sw, s3sw;
            string name = inst.Name;
            string vec;
            InstFlags flags = inst.Flags;
            DstOperand dOp = inst.DOp;
            CC cc = dOp.CC;
            CCOp ccOp = cc.Op;
            bool hcc = ccOp != CCOp.None;
            Modifier m = Modifier.FLOAT;

            dms = "";
            if (dOp.Data.Var != Var.None)
                getInstDstOp(dOp);
            else
                dn = null;

            if ((inst.Flags & InstFlags.I) != 0)
                m = Modifier.INT;
            else if ((inst.Flags & InstFlags.U) != 0)
                m = Modifier.UINT;

            bool addEnd = true;
            switch (inst)
            {
                case ABS abs:
                    getInstSrcOp(abs.SOp, dms);

                    writeInst(dms, "abs(", ")");
                    break;
                case ADD add:
                    getInstSrc1Op(add.SOp1, dms);
                    getInstSrc2Op(add.SOp2, dms);

                    writeInst2(dms, "", " + ", "");
                    break;
                case ARL arl:
                    getInstSrcOp(arl.SOp, "xyzw");

                    writeInstAction(dms, () => {
                        WriteString(s, "clamp(ivec4(floor(");
                        writeSrcDms(s, st, sn, ssw, "");
                        WriteString(s, ")), ivec4(-512), ivec4(511))");
                    });
                    break;
                case ARR arr:
                    getInstSrcOp(arr.SOp, "xyzw");

                    writeInstAction(dms, () => {
                        WriteString(s, "clamp(ivec4(round(");
                        writeSrcDms(s, st, sn, ssw, "");
                        WriteString(s, ")), ivec4(-512), ivec4(511))");
                    });
                    break;
                case CEIL ceil:
                    getInstSrcOp(ceil.SOp, dms);

                    writeInst(dms, "ceil(", ")");
                    break;
                case CMP cmp:
                    getInstSrc1Op(cmp.SOp1, dms);
                    getInstSrc2Op(cmp.SOp2, dms);
                    getInstSrc3Op(cmp.SOp3, dms);

                    writeInst3(dms, "mix(", ", ", ", lessThan(", ", vec4(0)))");
                    break;
                case COS cos:
                    getInstSrcOp(cos.SOp, "x");

                    writeInstInner(dms, "x", m, "cos", "");
                    break;
                case DDX ddx:
                    getInstSrcOp(ddx.SOp, dms);

                    writeInst(dms, "dFdx(", ")");
                    break;
                case DDY ddy:
                    getInstSrcOp(ddy.SOp, dms);

                    writeInst(dms, "dFdy(", ")");
                    break;
                case DIV div:
                    getInstSrc1Op(div.SOp1, dms);
                    getInstSrc2Op(div.SOp2, "x");

                    writeInstAction(dms, () => {
                        WriteString(s, "");
                        writeSrcDms(s, s1t, s1n, s1sw, dms);
                        WriteString(s, " / ");
                        writeSrcDms(s, s2t, s2n, s2sw, "x");
                        WriteString(s, "");
                    });
                    break;
                case DP2 dp2:
                    getInstSrc1Op(dp2.SOp1, "xy");
                    getInstSrc2Op(dp2.SOp2, "xy");

                    writeInstInnerAction(dms, m, () => {
                        WriteString(s, "dot(");
                        writeSrcDms(s, s1t, s1n, s1sw, "xy");
                        WriteString(s, ", ");
                        writeSrcDms(s, s2t, s2n, s2sw, "xy");
                        WriteString(s, ")");
                    });
                    break;
                case DP2A dp2a:
                    getInstSrc1Op(dp2a.SOp1, "xy");
                    getInstSrc2Op(dp2a.SOp2, "xy");
                    getInstSrc3Op(dp2a.SOp3, "x");

                    writeInstInnerAction(dms, m, () => {
                        WriteString(s, "dot(");
                        writeSrcDms(s, s1t, s1n, s1sw, "xy");
                        WriteString(s, ", ");
                        writeSrcDms(s, s2t, s2n, s2sw, "xy");
                        WriteString(s, ") + ");
                        writeSrcDms(s, s3t, s3n, s3sw, "x");
                        WriteString(s, "");
                    });
                    break;
                case DP3 dp3:
                    getInstSrc1Op(dp3.SOp1, "xyz");
                    getInstSrc2Op(dp3.SOp2, "xyz");

                    writeInstInnerAction(dms, m, () => {
                        WriteString(s, "dot(");
                        writeSrcDms(s, s1t, s1n, s1sw, "xyz");
                        WriteString(s, ", ");
                        writeSrcDms(s, s2t, s2n, s2sw, "xyz");
                        WriteString(s, ")");
                    });
                    break;
                case DP4 dp4:
                    getInstSrc1Op(dp4.SOp1, "xyzw");
                    getInstSrc2Op(dp4.SOp2, "xyzw");

                    writeInstInnerAction(dms, m, () => {
                        WriteString(s, "dot(");
                        writeSrcDms(s, s1t, s1n, s1sw, "");
                        WriteString(s, ", ");
                        writeSrcDms(s, s2t, s2n, s2sw, "");
                        WriteString(s, ")");
                    });
                    break;
                case DPH dph:
                    getInstSrc1Op(dph.SOp1, "xyz");
                    getInstSrc2Op(dph.SOp2, "xyz");
                    getInstSrc3Op(dph.SOp2, "xyzw");

                    writeInstInnerAction(dms, m, () => {
                        WriteString(s, "dot(");
                        writeSrcDms(s, s1t, s1n, s1sw, "xyz");
                        WriteString(s, ", ");
                        writeSrcDms(s, s2t, s2n, s2sw, "xyz");
                        WriteString(s, ") + ");
                        writeSrcDmsEl(s, s3t, s3n, s3sw, "xyzw", 3);
                        WriteString(s, "");
                    });
                    break;
                case DST dst:
                    getInstSrc1Op(dst.SOp1, "xyzw");
                    getInstSrc2Op(dst.SOp2, "xyzw");

                    WriteString(s, tab);
                    WriteString(s, $"vec4 tmp_{i1} = ");
                    writeSrcDms(s, s1t, s1n, s1sw, "");
                    WriteString(s, $";\n{tab}");
                    WriteString(s, $"vec4 tmp_{i1 + 1} = ");
                    writeSrcDms(s, s2t, s2n, s2sw, "");
                    WriteString(s, ";\n");

                    writeInstAction(dms, () => {
                        WriteString(s, $"vec4(1.0, tmp_{i1}.y * tmp_{i1 + 1}.y, tmp_{i1}.z, tmp_{i1 + 1}.w)");
                    });
                    i1 += 2;
                    break;
                case EX2 ex2:
                    getInstSrcOp(ex2.SOp, "x");

                    writeInstInner(dms, "x", m, "exp2", "");
                    break;
                case EXP exp:
                    getInstSrcOp(exp.SOp, "x");

                    WriteString(s, tab);
                    WriteString(s, $"float tmp_{i1} = ");
                    writeSrcDms(s, st, sn, ssw, "x");
                    WriteString(s, ";\n");

                    writeInstAction(dms, () => {
                        WriteString(s, $"vec4(pow(2, floor(tmp_{i1})), fract(tmp_{i1}), exp2(tmp_{i1}), 1)");
                    });
                    i1++;
                    break;
                case FLR flr:
                    getInstSrcOp(flr.SOp, dms);

                    writeInst(dms, "floor(", ")");
                    break;
                case FRC frc:
                    getInstSrcOp(frc.SOp, dms);

                    writeInst(dms, "fract(", ")");
                    break;
                case LG2 lg2:
                    getInstSrcOp(lg2.SOp, "x");

                    writeInstInner(dms, "x", m, "log2(", ")");
                    break;
                case LIT lit:
                    getInstSrcOp(lit.SOp, "xyzw");

                    WriteString(s, tab);
                    WriteString(s, $"vec4 tmp_{i1} = ");
                    writeSrcDms(s, st, sn, ssw, "");
                    WriteString(s, $";\n{tab}");
                    WriteString(s, $"tmp_{i1}.xy = max(tmp_{i1}.xy, vec2(0))");
                    WriteString(s, $";\n{tab}");
                    WriteString(s, $"tmp_{i1}.w = clamp(tmp_{i1}.w, -(128 - (1.0 / 256)), (128 - (1.0 / 256)))");
                    WriteString(s, ";\n");

                    writeInstAction(dms, () => {
                        WriteString(s, $"vec4(1, tmp_{i1}.x, tmp_{i1}.x > 0 ? ");
                        WriteString(s, $"exp2(tmp_{i1}.w * log2(tmp_{i1}.y)) : 0, 1)");
                    });
                    i1++;
                    break;
                case LOG log:
                    getInstSrcOp(log.SOp, "x");

                    WriteString(s, tab);
                    WriteString(s, $"float tmp_{i1} = fabs(");
                    writeSrcDms(s, st, sn, ssw, "x");
                    WriteString(s, ");\n");

                    writeInstAction(dms, () => {
                        WriteString(s, $"vec4(floor(log2(tmp_{i1})), ");
                        WriteString(s, $"tmp * (1 / floor(log2(tmp_{i1}))), log2(tmp_{i1}), 1)");
                    });
                    i1++;
                    break;
                case LRP lrp:
                    getInstSrc1Op(lrp.SOp3, dms);
                    getInstSrc2Op(lrp.SOp2, dms);
                    getInstSrc3Op(lrp.SOp1, dms);

                    writeInst3(dms, "mix(", ", ", ", ", ")");
                    break;
                case MAD mad:
                    getInstSrc1Op(mad.SOp1, dms);
                    getInstSrc2Op(mad.SOp2, dms);
                    getInstSrc3Op(mad.SOp3, dms);

                    writeInst3(dms, "", " * ", " + ", "");
                    break;
                case MAX max:
                    getInstSrc1Op(max.SOp1, dms);
                    getInstSrc2Op(max.SOp2, dms);

                    writeInst2(dms, "max(", ", ", ")");
                    break;
                case MIN min:
                    getInstSrc1Op(min.SOp1, dms);
                    getInstSrc2Op(min.SOp2, dms);

                    writeInst2(dms, "min(", ", ", ")");
                    break;
                case MOV mov:
                    getInstSrcOp(mov.SOp, dms);

                    writeInst(dms, "", "");
                    break;
                case MUL mul:
                    getInstSrc1Op(mul.SOp1, dms);
                    getInstSrc2Op(mul.SOp2, dms);

                    writeInst2(dms, "", " * ", "");
                    break;
                case NRM nrm:
                    getInstSrcOp(nrm.SOp, "xyz");

                    writeInstAction(dms, () => {
                        WriteString(s, "vec4(normalize(");
                        writeSrcDms(s, st, sn, ssw, "xyz");
                        WriteString(s, "), 0)");
                    });
                    break;
                case POW pow:
                    getInstSrc1Op(pow.SOp1, "x");
                    getInstSrc2Op(pow.SOp2, "x");

                    writeInstInnerAction(dms, m, () => {
                        WriteString(s, "pow(");
                        writeSrcDms(s, s1t, s1n, s1sw, "x");
                        WriteString(s, ", ");
                        writeSrcDms(s, s2t, s2n, s2sw, "x");
                        WriteString(s, ")");
                    });
                    break;
                case RCC rcc:
                    getInstSrcOp(rcc.SOp, dms);

                    WriteString(s, tab);
                    WriteString(s, $"float tmp_{i1} = 1 / ");
                    writeSrcDms(s, st, sn, ssw, "x");
                    WriteString(s, $";\n{tab}");
                    WriteString(s, $"tmp_{i1} = tmp_{i1} > 0 ? clamp(tmp_{i1}, 5.42101e-020, 1.84467e+019)");
                    WriteString(s, $" : clamp(tmp_{i1}, -1.84467e+019, -5.42101e-020)");
                    WriteString(s, ";\n");

                    writeInstInnerAction(dms, m, () => {
                        WriteString(s, $"tmp_{i1}");
                    });
                    i1++;
                    break;
                case RCP rcp:
                    getInstSrcOp(rcp.SOp, dms);

                    writeInstInnerAction(dms, m, () => {
                        WriteString(s, "1 / ");
                        writeSrcDms(s, st, sn, ssw, "x");
                        WriteString(s, "");
                    });
                    break;
                case RFL rfl:
                    getInstSrc1Op(rfl.SOp1, dms);
                    getInstSrc2Op(rfl.SOp2, dms);

                    writeInst2(dms, "reflect(", ", ", ")");
                    break;
                case RSQ rsq:
                    getInstSrcOp(rsq.SOp, dms);

                    writeInstInnerAction(dms, m, () => {
                        WriteString(s, "inversesqrt(");
                        writeSrcDms(s, st, sn, ssw, "x");
                        WriteString(s, ")");
                    });
                    break;
                case SCS scs:
                    getInstSrcOp(scs.SOp, "x");

                    writeInstInner2(dms, "x", m, "cos", "sin", "");
                    break;
                case SFL sfl:
                    writeInstInnerAction(dms, m, () => {
                        WriteString(s, "0");
                    });
                    break;
                case SGE sge:
                    getInstSrc1Op(sge.SOp1, "xyzw");
                    getInstSrc2Op(sge.SOp2, "xyzw");

                    vec = getTypeMod(Modifier.FLOAT, 4);
                    writeInstAction(dms, () => {
                        WriteString(s, $"mix({vec}(1), {vec}(0), greaterThanEqual(");
                        writeSrcDms(s, s1t, s1n, s1sw, "");
                        WriteString(s, ", ");
                        writeSrcDms(s, s2t, s2n, s2sw, "");
                        WriteString(s, "))");
                    });
                    break;
                case SGT sgt:
                    getInstSrc1Op(sgt.SOp1, "xyzw");
                    getInstSrc2Op(sgt.SOp2, "xyzw");

                    vec = getTypeMod(Modifier.FLOAT, 4);
                    writeInstAction(dms, () => {
                        WriteString(s, $"mix({vec}(1), {vec}(0), greaterThan(");
                        writeSrcDms(s, s1t, s1n, s1sw, "");
                        WriteString(s, ", ");
                        writeSrcDms(s, s2t, s2n, s2sw, "");
                        WriteString(s, "))");
                    });
                    break;
                case SIN sin:
                    getInstSrcOp(sin.SOp, "x");

                    writeInstInner(dms, "x", m, "sin", "");
                    break;
                case SLE sle:
                    getInstSrc1Op(sle.SOp1, "xyzw");
                    getInstSrc2Op(sle.SOp2, "xyzw");

                    vec = getTypeMod(Modifier.FLOAT, 4);
                    writeInstAction(dms, () => {
                        WriteString(s, $"mix({vec}(1), {vec}(0), lessThanEqual(");
                        writeSrcDms(s, s1t, s1n, s1sw, "");
                        WriteString(s, ", ");
                        writeSrcDms(s, s2t, s2n, s2sw, "");
                        WriteString(s, "))");
                    });
                    break;
                case SLT slt:
                    getInstSrc1Op(slt.SOp1, "xyzw");
                    getInstSrc2Op(slt.SOp2, "xyzw");

                    vec = getTypeMod(Modifier.FLOAT, 4);
                    writeInstAction(dms, () => {
                        WriteString(s, $"mix({vec}(1), {vec}(0), lessThan(");
                        writeSrcDms(s, s1t, s1n, s1sw, "");
                        WriteString(s, ", ");
                        writeSrcDms(s, s2t, s2n, s2sw, "");
                        WriteString(s, "))");
                    });
                    break;
                case SNE sne:
                    getInstSrc1Op(sne.SOp1, "xyzw");
                    getInstSrc2Op(sne.SOp2, "xyzw");

                    vec = getTypeMod(Modifier.FLOAT, 4);
                    writeInstAction(dms, () => {
                        WriteString(s, $"mix({vec}(1), {vec}(0), notEqual(");
                        writeSrcDms(s, s1t, s1n, s1sw, "");
                        WriteString(s, ", ");
                        writeSrcDms(s, s2t, s2n, s2sw, "");
                        WriteString(s, "))");
                    });
                    break;
                case SSG ssg:
                    getInstSrcOp(ssg.SOp, "xyzw");

                    writeInstAction(dms, () => {
                        WriteString(s, "mix(mix(vec4(0), vec4(-1), lessThan(");
                        writeSrcDms(s, st, sn, ssw, "");
                        WriteString(s, ", vec4(0))), vec4(1), greaterThan(");
                        writeSrcDms(s, st, sn, ssw, "");
                        WriteString(s, ", vec4(0)))");
                    });
                    break;
                case STR str:
                    writeInstInnerAction(dms, m, () => {
                        WriteString(s, "1");
                    });
                    break;
                case SUB sub:
                    getInstSrc1Op(sub.SOp1, dms);
                    getInstSrc2Op(sub.SOp2, dms);

                    writeInst2(dms, "", " - ", "");
                    break;
                case SWZ swz:
                    getInstSrcOp(swz.SOp, dms);

                    writeInstAction(dms, () => {
                        if (swz.XSwizzle != "x" || swz.YSwizzle != "y" || swz.ZSwizzle != "z" || swz.WSwizzle != "w")
                        {
                            WriteString(s, "vec4(");
                            getSWZInstSwizzle(s, st, sn, ssw, swz.XSwizzle, swz.Negate, ", ");
                            getSWZInstSwizzle(s, st, sn, ssw, swz.YSwizzle, swz.Negate, ", ");
                            getSWZInstSwizzle(s, st, sn, ssw, swz.ZSwizzle, swz.Negate, ", ");
                            getSWZInstSwizzle(s, st, sn, ssw, swz.WSwizzle, swz.Negate, ")");

                            writeSrcDms(s, st, sn, ssw, dms);

                            static void getSWZInstSwizzle(Stream s, IType st,
                                string sn, string ssw, string swizzle, bool negate, string t)
                            {
                                     if (swizzle == "1") WriteString(s, negate ? "-1" : "1");
                                else if (swizzle == "0") WriteString(s, negate ? "-0" : "0");
                                else writeSrcDms(s, st, sn, ssw, swizzle);
                                WriteString(s, t);
                            }
                        }
                    });
                    break;
                case TEX tex:
                    getInstSrcOp(tex.UV,
                        tex.UV.Data.Var switch
                        {
                            Var.Value   => "x",
                            Var.Vector2 => "xy",
                            Var.Vector3 => "xyz",
                            _ => null,
                        });

                    writeInstAction(dms, () => {
                        string texMask = tex.Mode switch
                        {
                            TexMode._1D => "x",
                            TexMode._2D => "xy",
                            TexMode._3D => "xyz",
                            TexMode._CUBE => "xyz",
                            TexMode._RECT => "xy",
                            TexMode._SHADOW1D => "xyz",
                            TexMode._SHADOW2D => "xyz",
                            TexMode._SHADOWRECT => "xyz",
                            _ => throw new Exception("0x0017 Invalid texture mode"),
                        };

                        name = tex.Mode switch
                        {
                            TexMode._1D => "1D",
                            TexMode._2D => "2D",
                            TexMode._3D => "3D",
                            TexMode._CUBE => "CUBE",
                            TexMode._RECT => "RECT",
                            TexMode._SHADOW1D => "SHADOW1D",
                            TexMode._SHADOW2D => "SHADOW2D",
                            TexMode._SHADOWRECT => "SHADOWRECT",
                            _ => throw new Exception("0x0018 Invalid texture mode"),
                        };

                        switch (tex.Mode)
                        {
                            case TexMode._SHADOW1D:
                            case TexMode._SHADOW2D:
                            case TexMode._SHADOWRECT: WriteString(s, "vec4("); break;
                        }

                        if (tex.Offset != null)
                        {
                            WriteString(s, $"textureOffset(Texture{name}{tex.Index}, ");
                            writeSrcDms(s, st, sn, ssw, texMask);
                            WriteString(s, ", ");
                            WriteString(s,
                                tex.Mode switch
                                {
                                    TexMode._1D => "",
                                    TexMode._2D => "ivec2",
                                    TexMode._3D => "ivec3",
                                    TexMode._RECT => "ivec2",
                                    TexMode._SHADOW1D => "vec2",
                                    TexMode._SHADOW2D => "ivec2",
                                    TexMode._SHADOWRECT => "ivec2",
                                    _ => throw new Exception("0x0092 Invalid texture mode while trying to use offset"),
                                });
                            WriteString(s, tex.Offset);
                            WriteString(s, ")");
                        }
                        else
                        {
                            WriteString(s, $"texture(Texture{name}{tex.Index}, ");
                            writeSrcDms(s, st, sn, ssw, texMask);
                            WriteString(s, ")");
                        }

                        switch (tex.Mode)
                        {
                            case TexMode._SHADOW1D:
                            case TexMode._SHADOW2D:
                            case TexMode._SHADOWRECT: WriteString(s, ")"); break;
                        }
                    });
                    break;
                case TRUNC trunc:
                    getInstSrcOp(trunc.SOp, dms);

                    writeInst(dms, "trunc(", ")");
                    break;
                case TXB txb:
                    getInstSrcOp(txb.UV,
                        txb.UV.Data.Var switch
                        {
                            Var.Value   => "x",
                            Var.Vector2 => "xy",
                            Var.Vector3 => "xyz",
                            _ => null,
                        });

                    writeInstAction(dms, () => {
                        string texMask = txb.Mode switch
                        {
                            TexMode._1D => "x",
                            TexMode._2D => "xy",
                            TexMode._3D => "xyz",
                            TexMode._CUBE => "xyz",
                            TexMode._RECT => "xy",
                            TexMode._SHADOW1D => "xyz",
                            TexMode._SHADOW2D => "xyz",
                            TexMode._SHADOWRECT => "xyz",
                            _ => throw new Exception("0x005A Invalid texture mode"),
                        };

                        name = txb.Mode switch
                        {
                            TexMode._1D => "1D",
                            TexMode._2D => "2D",
                            TexMode._3D => "3D",
                            TexMode._CUBE => "CUBE",
                            TexMode._RECT => "RECT",
                            TexMode._SHADOW1D => "SHADOW1D",
                            TexMode._SHADOW2D => "SHADOW2D",
                            TexMode._SHADOWRECT => "SHADOWRECT",
                            _ => throw new Exception("0x005B Invalid texture mode"),
                        };

                        switch (txb.Mode)
                        {
                            case TexMode._SHADOW1D:
                            case TexMode._SHADOW2D:
                            case TexMode._SHADOWRECT: WriteString(s, "vec4("); break;
                        }

                        if (txb.Offset != null)
                        {
                            WriteString(s, $"textureOffset(Texture{name}{txb.Index}, ");
                            writeSrcDms(s, st, sn, ssw, texMask);
                            WriteString(s, ", ");
                            writeSrcDmsEl(s, st, sn, ssw, "xyzw", 3);
                            WriteString(s, ", ");
                            WriteString(s,
                                txb.Mode switch
                                {
                                    TexMode._1D => "",
                                    TexMode._2D => "ivec2",
                                    TexMode._3D => "ivec3",
                                    TexMode._RECT => "ivec2",
                                    TexMode._SHADOW1D => "vec2",
                                    TexMode._SHADOW2D => "ivec2",
                                    TexMode._SHADOWRECT => "ivec2",
                                    _ => throw new Exception("0x0093 Invalid texture mode while trying to use offset"),
                                });
                            WriteString(s, txb.Offset);
                            WriteString(s, ")");
                        }
                        else
                        {
                            WriteString(s, $"texture(Texture{name}{txb.Index}, ");
                            writeSrcDms(s, st, sn, ssw, texMask);
                            WriteString(s, ", ");
                            writeSrcDmsEl(s, st, sn, ssw, "xyzw", 3);
                            WriteString(s, ")");
                        }

                        switch (txb.Mode)
                        {
                            case TexMode._SHADOW1D:
                            case TexMode._SHADOW2D:
                            case TexMode._SHADOWRECT: WriteString(s, ")"); break;
                        }
                    });
                    break;
                case TXL txl:
                    getInstSrcOp(txl.UV,
                        txl.UV.Data.Var switch
                        {
                            Var.Value   => "x",
                            Var.Vector2 => "xy",
                            Var.Vector3 => "xyz",
                            _ => null,
                        });

                    writeInstAction(dms, () => {
                        string texMask = txl.Mode switch
                        {
                            TexMode._1D => "x",
                            TexMode._2D => "xy",
                            TexMode._3D => "xyz",
                            TexMode._CUBE => "xyz",
                            TexMode._RECT => throw new Exception("0x0055 RECT is unsupported by textureLod"),
                            TexMode._SHADOW1D => "xyz",
                            TexMode._SHADOW2D => "xyz",
                            TexMode._SHADOWRECT =>
                                throw new Exception("0x0056 SHADOWRECT is unsupported by textureLod"),
                            _ => throw new Exception("0x0019 Invalid texture mode"),
                        };

                        name = txl.Mode switch
                        {
                            TexMode._1D => "1D",
                            TexMode._2D => "2D",
                            TexMode._3D => "3D",
                            TexMode._CUBE => "CUBE",
                            TexMode._RECT => throw new Exception("0x0057 RECT is unsupported by textureLod"),
                            TexMode._SHADOW1D => "SHADOW1D",
                            TexMode._SHADOW2D => "SHADOW2D",
                            TexMode._SHADOWRECT =>
                                throw new Exception("0x0058 SHADOWRECT is unsupported by textureLod"),
                            _ => throw new Exception("0x001A Invalid texture mode"),
                        };

                        switch (txl.Mode)
                        {
                            case TexMode._SHADOW1D:
                            case TexMode._SHADOW2D: WriteString(s, "vec4("); break;
                        }

                        if (txl.Offset != null)
                        {
                            WriteString(s, $"textureLodOffset(Texture{name}{txl.Index}, ");
                            writeSrcDms(s, st, sn, ssw, texMask);
                            WriteString(s, ", ");
                            writeSrcDmsEl(s, st, sn, ssw, "xyzw", 3);
                            WriteString(s, ", ");
                            WriteString(s,
                                txl.Mode switch
                                {
                                    TexMode._1D => "",
                                    TexMode._2D => "ivec2",
                                    TexMode._3D => "ivec3",
                                    TexMode._SHADOW1D => "vec2",
                                    TexMode._SHADOW2D => "ivec2",
                                    _ => throw new Exception("0x0094 Invalid texture mode while trying to use offset"),
                                });
                            WriteString(s, txl.Offset);
                            WriteString(s, ")");
                        }
                        else
                        {
                            WriteString(s, $"textureLod(Texture{name}{txl.Index}, ");
                            writeSrcDms(s, st, sn, ssw, texMask);
                            WriteString(s, ", ");
                            writeSrcDmsEl(s, st, sn, ssw, "xyzw", 3);
                            WriteString(s, ")");
                        }

                        switch (txl.Mode)
                        {
                            case TexMode._SHADOW1D:
                            case TexMode._SHADOW2D: WriteString(s, ")"); break;
                        }
                    });
                    break;
                case TXP txp:
                    getInstSrcOp(txp.UV,
                        txp.UV.Data.Var switch
                        {
                            Var.Value   => "x",
                            Var.Vector2 => "xy",
                            Var.Vector3 => "xyz",
                            _ => null,
                        });

                    writeInstAction(dms, () => {
                        name = txp.Mode switch
                        {
                            TexMode._1D => "1D",
                            TexMode._2D => "2D",
                            TexMode._3D => "3D",
                            TexMode._CUBE => "CUBE",
                            TexMode._RECT => "RECT",
                            TexMode._SHADOW1D => "SHADOW1D",
                            TexMode._SHADOW2D => "SHADOW2D",
                            TexMode._SHADOWRECT => "SHADOWRECT",
                            _ => throw new Exception("0x0059 Invalid texture mode"),
                        };

                        switch (txp.Mode)
                        {
                            case TexMode._SHADOW1D:
                            case TexMode._SHADOW2D:
                            case TexMode._SHADOWRECT: WriteString(s, "vec4("); break;
                        }

                        if (txp.Offset != null)
                        {
                            WriteString(s, $"textureProjOffset(Texture{name}{txp.Index}, ");
                            writeSrcDms(s, st, sn, ssw, "");
                            WriteString(s, ", ");
                            WriteString(s,
                                txp.Mode switch
                                {
                                    TexMode._1D => "",
                                    TexMode._2D => "ivec2",
                                    TexMode._3D => "ivec3",
                                    TexMode._RECT => "ivec2",
                                    TexMode._SHADOW1D => "vec2",
                                    TexMode._SHADOW2D => "ivec2",
                                    TexMode._SHADOWRECT => "ivec2",
                                    _ => throw new Exception("0x0095 Invalid texture mode while trying to use offset"),
                                });
                            WriteString(s, txp.Offset);
                            WriteString(s, ")");
                        }
                        else
                        {
                            WriteString(s, $"textureProj(Texture{name}{txp.Index}, ");
                            writeSrcDms(s, st, sn, ssw, "");
                            WriteString(s, ")");
                        }

                        switch (txp.Mode)
                        {
                            case TexMode._SHADOW1D:
                            case TexMode._SHADOW2D:
                            case TexMode._SHADOWRECT: WriteString(s, ")"); break;
                        }
                    });
                    break;
                case X2D x2d:
                    getInstSrc1Op(x2d.SOp1, "xyzw");
                    getInstSrc2Op(x2d.SOp2, "xyzw");
                    getInstSrc3Op(x2d.SOp3, "xyzw");

                    WriteString(s, tab);
                    WriteString(s, $"vec4 tmp_{i1} = ");
                    writeSrcDms(s, s1t, s1n, s1sw, "");
                    WriteString(s, $";\n{tab}");
                    WriteString(s, $"vec4 tmp_{i1 + 1} = ");
                    writeSrcDms(s, s2t, s2n, s2sw, "");
                    WriteString(s, $";\n{tab}");
                    WriteString(s, $"vec4 tmp_{i1 + 2} = ");
                    writeSrcDms(s, s3t, s3n, s3sw, "");
                    WriteString(s, ";\n");

                    writeInstAction(dms, () => {
                        WriteString(s, "vec4(vec2(");
                        WriteString(s, $"tmp_{i1}.x + tmp_{i1 + 1}.x * tmp_{i1 + 2}.x");
                        WriteString(s, $"+ tmp_{i1 + 1}.y * tmp_{i1 + 2}.y");
                        WriteString(s, ", ");
                        WriteString(s, $"tmp_{i1}.y + tmp_{i1 + 1}.x * tmp_{i1 + 2}.z");
                        WriteString(s, $" + tmp_{i1 + 1}.y * tmp_{i1 + 2}.y");
                        WriteString(s, ").xyxy)");
                    });
                    i1 += 3;
                    break;
                case XPD xpd:
                    getInstSrc1Op(xpd.SOp1, "xyz");
                    getInstSrc2Op(xpd.SOp2, "xyz");

                    writeInstAction(dms, () => {
                        WriteString(s, "vec4(cross(");
                        writeSrcDms(s, s1t, s1n, s1sw, "xyz");
                        WriteString(s, ", ");
                        writeSrcDms(s, s2t, s2n, s2sw, "xyz");
                        WriteString(s, "), 0)");
                    });
                    break;
                case BRA bra:
                    addEnd = false;
                    cc = bra.CC;
                    ccOp = cc.Op;

                    instLevel++;
                    WriteString(s, tab);
                    WriteString(s, "if (");
                    writeCCComp(cc.Swizzle, "!");
                    WriteString(s, ")\n");
                    WriteString(s, tab);
                    WriteString(s, "{\n");
                    i++;
                    tmpNum = i1;
                    WriteInsts(tab + Tab, ref i, ref i0, ref tmpNum);
                    i1 = tmpNum;
                    i--;
                    break;
                case CAL cal:
                    addEnd = false;
                    cc = cal.CC;
                    ccOp = cc.Op;

                    instLevel++;
                    WriteString(s, tab);
                    WriteString(s, "if (");
                    writeCCComp(cc.Swizzle, "");
                    WriteString(s, $") // {cal.Label}\n");
                    WriteString(s, tab);
                    WriteString(s, "{\n");
                    i++;
                    for (i2 = i; i2 < i0; i2++)
                        if (this.inst[i2] is Label)
                            break;
                    if (i2 == i0)
                        throw new Exception("0x009E Invalid");

                    tmpNum = i2 + 1;
                    i2 = i;
                    i = tmpNum;
                    tmpNum = i1;
                    WriteInsts(tab + Tab, ref i, ref i0, ref tmpNum, true);
                    i1 = tmpNum;
                    i = i2;
                    i--;
                    break;
                case ELSE @else:
                    addEnd = false;
                    for (i2 = 0; i2 < instLevel; i2++)
                        WriteString(s, Tab);
                    WriteString(s, "}\n");
                    for (i2 = 0; i2 < instLevel; i2++)
                        WriteString(s, Tab);
                    WriteString(s, "else\n");
                    for (i2 = 0; i2 < instLevel; i2++)
                        WriteString(s, Tab);
                    WriteString(s, "{\n");
                    break;
                case ENDIF endif:
                    addEnd = false;
                    for (i2 = 0; i2 < instLevel; i2++)
                        WriteString(s, Tab);
                    WriteString(s, "}\n");
                    instLevel--;
                    @return = true;
                    break;
                case ENDLOOP endloop:
                    addEnd = false;
                    for (i2 = 0; i2 < instLevel; i2++)
                        WriteString(s, Tab);
                    WriteString(s, "}\n");
                    instLevel--;
                    @return = true;
                    break;
                case ENDREP endrep:
                    addEnd = false;
                    for (i2 = 0; i2 < instLevel; i2++)
                        WriteString(s, Tab);
                    WriteString(s, "}\n");
                    instLevel--;
                    @return = true;
                    break;
                case IF @if:
                    addEnd = false;
                    cc = @if.CC;
                    ccOp = cc.Op;

                    instLevel++;
                    WriteString(s, tab);
                    WriteString(s, "if (");
                    writeCCComp(cc.Swizzle, "");
                    WriteString(s, ")\n");
                    WriteString(s, tab);
                    WriteString(s, "{\n");
                    i++;
                    tmpNum = i1;
                    WriteInsts(tab + Tab, ref i, ref i0, ref tmpNum);
                    i1 = tmpNum;
                    break;
                case KIL kil:
                    if (kil.CC.Op == CCOp.None)
                    {
                        getInstSrcOp(kil.SOp, "xyzw");

                        bool hx = false, hy = false, hz = false, hw = false;
                        WriteString(s, tab);
                        WriteString(s, "if (");
                        writeSrc(s, st, sn, $"{ssw[0]}");
                        WriteString(s, " < 0");
                        addHasComp(ssw[0]);
                        writeComp(ssw[1]);
                        writeComp(ssw[2]);
                        writeComp(ssw[3]);
                        WriteString(s, $")\n{tab}{Tab}discard");

                        void writeComp(char c)
                        {
                            if (addHasComp(c)) return;

                            WriteString(s, " || ");
                            writeSrc(s, st, sn, $"{c}");
                            WriteString(s, " < 0");
                        }

                        bool addHasComp(char c)
                        {
                                 if (c == 'x') { if (hx) return true; else { hx = true; return false; } }
                            else if (c == 'y') { if (hy) return true; else { hy = true; return false; } }
                            else if (c == 'z') { if (hz) return true; else { hz = true; return false; } }
                            else if (c == 'w') { if (hw) return true; else { hw = true; return false; } }
                            else if (c == 'r') { if (hx) return true; else { hx = true; return false; } }
                            else if (c == 'g') { if (hy) return true; else { hy = true; return false; } }
                            else if (c == 'b') { if (hz) return true; else { hz = true; return false; } }
                            else if (c == 'a') { if (hw) return true; else { hw = true; return false; } }
                            else throw new Exception("0x008F Invalid");
                        }
                    }
                    else
                    {
                        cc = kil.CC;
                        ccOp = cc.Op;

                        WriteString(s, tab);
                        WriteString(s, "if (");
                        writeCCComp(cc.Swizzle, "");
                        WriteString(s, $")\n{tab}{Tab}discard");
                    }
                    break;
                case Label lbl:
                    for (i2 = 0; i2 < label.Count; i2++)
                        if (label[i2] == lbl.Name)
                            break;
                    if (i2 == label.Count)
                        throw new Exception("0x0099 Invalid");
                    label.RemoveAt(i2);

                    addEnd = false;
                    for (i2 = 0; i2 < instLevel; i2++)
                        WriteString(s, Tab);
                    instLevel--;
                    @return = true;
                    if (instLevel > 0)
                        WriteString(s, "}\n");
                    else
                    {
                        WriteString(s, $"{"}"} // {lbl.Name}\n");
                        i++;
                    }
                    break;
                case LOOP loop:
                    getInstSrcOp(loop.SOp, "xyz");

                    WriteString(s, tab);
                    WriteString(s, $"ivec3 tmp_{i1} = ivec3(floor(");
                    writeSrcDms(s, st, sn, ssw, "xyz");
                    WriteString(s, $"));\n");

                    addEnd = false;

                    instLevel++;
                    WriteString(s, tab);
                    WriteString(s, "for (");
                    WriteString(s, $"int tmp_{i1 + 1} = tmp_{i1}.y; ");
                    WriteString(s, $"tmp_{i1 + 1} < tmp_{i1}.x; ");
                    WriteString(s, $"tmp_{i1 + 1} += tmp_{i1}.z");
                    WriteString(s, ")\n");
                    WriteString(s, tab);
                    WriteString(s, "{\n");
                    i++;
                    tmpNum = i1 + 2;
                    WriteInsts(tab + Tab, ref i, ref i0, ref tmpNum);
                    i1 = tmpNum;
                    break;
                case REP rep:
                    getInstSrcOp(rep.SOp, "x");

                    WriteString(s, tab);
                    WriteString(s, $"int tmp_{i1} = int(floor(");
                    writeSrcDms(s, st, sn, ssw, "x");
                    WriteString(s, $"));\n");

                    addEnd = false;

                    instLevel++;
                    WriteString(s, tab);
                    WriteString(s, "for (");
                    WriteString(s, $"int tmp_{i1 + 1} = 0; ");
                    WriteString(s, $"tmp_{i1 + 1} < tmp_{i1}; ");
                    WriteString(s, $"tmp_{i1 + 1}++");
                    WriteString(s, ")\n");
                    WriteString(s, tab);
                    WriteString(s, "{\n");
                    i++;
                    tmpNum = i1 + 2;
                    WriteInsts(tab + Tab, ref i, ref i0, ref tmpNum);
                    i1 = tmpNum;
                    break;
                case RET ret:
                    if (ret.CC.Op == CCOp.None)
                    {
                        if (instLevel > 0 && !call)
                        {
                            WriteString(s, tab);
                            WriteString(s, "return");
                            WriteString(s, ";\n");
                        }
                        if (call || instLevel > 0)
                        {
                            for (i2 = 0; i2 < instLevel; i2++)
                                WriteString(s, Tab);
                            WriteString(s, "}\n");
                            instLevel--;
                        }
                        @return = true;
                        addEnd = false;
                        hasReturn = true;
                        i++;
                    }
                    else
                    {
                        cc = ret.CC;
                        ccOp = cc.Op;

                        WriteString(s, tab);
                        WriteString(s, "if (");
                        writeCCComp(cc.Swizzle, "");
                        WriteString(s, $")\n{tab}{Tab}return");
                    }
                    break;
                default:
                    WriteString(s, tab);
                    WriteString(s, $"//{inst}");
                    failInstName = name;
                    break;
            }
            if (addEnd)
                WriteString(s, ";\n");

            if (   (inst.Flags & InstFlags.C  ) != 0
                || (inst.Flags & InstFlags.CC ) != 0
                || (inst.Flags & InstFlags.CC0) != 0
                || (inst.Flags & InstFlags.CC1) != 0)
            {
                string ccn = (inst.Flags & InstFlags.CC1) != 0 ? "cc1" : "cc0";
                int l = dms.Length;
                l = l == 0 ? 4 : l;
                WriteString(s, tab);
                if (l == 1)
                    WriteString(s, $"{ccn}.{dms} = GetCC({dn}.{dms})");
                else if (l == 4)
                    WriteString(s, $"{ccn} = GetCCVec({dn}, 4)");
                else
                    WriteString(s, $"{ccn}.{dms} = GetCCVec({dn}.{dms}, {l})");
                WriteString(s, ";\n");
            }
            tmpNum = i1;

            void writeCCComp(string ccSwizzle, string t)
            {
                bool hx = false, hy = false, hz = false, hw = false;
                string sw;
                if (ccSwizzle == null) sw = "xyzw";
                else if (ccSwizzle.Length == 1) { sw = ccSwizzle; sw = $"{sw}{sw}{sw}{sw}"; }
                else if (ccSwizzle.Length == 4) sw = ccSwizzle;
                else throw new Exception("0x0090 Invalid");
                WriteString(s, $"{t}B{getCCOp($"{sw[0]}")})");
                addHasComp(sw[0]);
                writeComp(sw[1]);
                writeComp(sw[2]);
                writeComp(sw[3]);

                void writeComp(char c)
                {
                    if (addHasComp(c)) return;

                    WriteString(s, " || ");
                    WriteString(s, $"{t}B{getCCOp($"{c}")})");
                }

                bool addHasComp(char c)
                {
                         if (c == 'x') { if (hx) return true; else { hx = true; return false; } }
                    else if (c == 'y') { if (hy) return true; else { hy = true; return false; } }
                    else if (c == 'z') { if (hz) return true; else { hz = true; return false; } }
                    else if (c == 'w') { if (hw) return true; else { hw = true; return false; } }
                            else throw new Exception("0x0091 Invalid");
                }
            }

            void getInstDstOp(DstOperand dop)
            {
                dt = dop.Data;
                dms = dop.Mask;
                if (dt.Var != Var.Name)
                    dn = TypeToString(dt, m);
                else if (dt is TypeName tn)
                {
                    string tmp = tn.Name;
                    if (match.ContainsKey(tmp))
                    {
                        m = matchMod[tmp];
                        dn = match[tmp];
                    }
                    else
                        throw new Exception($"0x0085 Invalid {name} dst op");
                }
                else if (dt is TypeIndexName tin)
                {
                    string tmp = $"{tin.Name}[{"{0}"}]";
                    if (match.ContainsKey(tmp))
                        dn = string.Format($"{match[tmp]}", tin.Index);
                    else
                        throw new Exception($"0x0086 Invalid {name} src op");
                }
                else
                    throw new Exception($"0x001C Invalid {name} dest op");

                if (dn == "gl_PointSize" || dn == "gl_FragDepth")
                    dms = "x";
            }

            void getInstSrcOp(SrcOperand sop, string dms)
            {
                st = sop.Data;
                ssw = sop.Swizzle;
                if (st.Var != Var.Name)
                    sn = TypeMaskToString(st, dms, m);
                else if (st is TypeName tn)
                {
                    string tmp = tn.Name;
                    if (match.ContainsKey(tmp))
                        sn = match[tmp];
                    else
                        throw new Exception($"0x0087 Invalid {name} src op");
                }
                else if (st is TypeIndexName tin)
                {
                    string tmp = $"{tin.Name}[{"{0}"}]";
                    if (match.ContainsKey(tmp))
                        sn = string.Format($"{match[tmp]}", tin.Index);
                    else
                        throw new Exception($"0x0088 Invalid {name} src op");
                }
                else
                    throw new Exception($"0x001D Invalid {name} src op");
            }

            void getInstSrc1Op(SrcOperand sop1, string dms)
            {
                s1t = sop1.Data;
                s1sw = sop1.Swizzle;
                if (s1t.Var != Var.Name)
                    s1n = TypeMaskToString(s1t, dms, m);
                else if (s1t is TypeName tn)
                {
                    string tmp = tn.Name;
                    if (match.ContainsKey(tmp))
                        s1n = match[tmp];
                    else
                        throw new Exception($"0x0089 Invalid {name} src 1 op");
                }
                else if (s1t is TypeIndexName tin)
                {
                    string tmp = $"{tin.Name}[{"{0}"}]";
                    if (match.ContainsKey(tmp))
                        s1n = string.Format($"{match[tmp]}", tin.Index);
                    else
                        throw new Exception($"0x008A Invalid {name} src 1 op");
                }
                else
                    throw new Exception($"0x001E Invalid {name} src 1 op");
            }

            void getInstSrc2Op(SrcOperand sop2, string dms)
            {
                s2t = sop2.Data;
                s2sw = sop2.Swizzle;
                if (s2t.Var != Var.Name)
                    s2n = TypeMaskToString(s2t, dms, m);
                else if (s2t is TypeName tn)
                {
                    string tmp = tn.Name;
                    if (match.ContainsKey(tmp))
                        s2n = match[tmp];
                    else
                        throw new Exception($"0x008B Invalid {name} src 2 op");
                }
                else if (s2t is TypeIndexName tin)
                {
                    string tmp = $"{tin.Name}[{"{0}"}]";
                    if (match.ContainsKey(tmp))
                        s2n = string.Format($"{match[tmp]}", tin.Index);
                    else
                        throw new Exception($"0x008C Invalid {name} src 2 op");
                }
                else
                    throw new Exception($"0x001F Invalid {name} src 2 op");
            }

            void getInstSrc3Op(SrcOperand sop3, string dms)
            {
                s3t = sop3.Data;
                s3sw = sop3.Swizzle;
                if (s3t.Var != Var.Name)
                    s3n = TypeMaskToString(s3t, dms, m);
                else if (s3t is TypeName tn)
                {
                    string tmp = tn.Name;
                    if (match.ContainsKey(tmp))
                        s3n = match[tmp];
                    else
                        throw new Exception($"0x008D Invalid {name} src 2 op");
                }
                else if (s3t is TypeIndexName tin)
                {
                    string tmp = $"{tin.Name}[{"{0}"}]";
                    if (match.ContainsKey(tmp))
                        s3n = string.Format($"{match[tmp]}", tin.Index);
                    else
                        throw new Exception($"0x008E Invalid {name} src 2 op");
                }
                else
                    throw new Exception($"0x0020 Invalid {name} src 3 op");
            }

            void writeInst(string dms, string t1, string t2)
            {
                WriteString(s, tab);
                int l = dms.Length;
                l = l == 0 ? 4 : l;
                writeDstCC(l, dms, () => {
                    WriteString(s, t1);
                    writeSrcDms(s, st, sn, ssw, dms);
                    WriteString(s, t2);
                });
            }

            void writeInst2(string dms, string t1, string t2, string t3)
            {
                WriteString(s, tab);
                int l = dms.Length;
                l = l == 0 ? 4 : l;
                writeDstCC(l, dms, () => {
                    WriteString(s, t1);
                    writeSrcDms(s, s1t, s1n, s1sw, dms);
                    WriteString(s, t2);
                    writeSrcDms(s, s2t, s2n, s2sw, dms);
                    WriteString(s, t3);
                });
            }

            void writeInst3(string dms, string t1, string t2, string t3, string t4)
            {
                WriteString(s, tab);
                int l = dms.Length;
                l = l == 0 ? 4 : l;
                writeDstCC(l, dms, () => {
                    WriteString(s, t1);
                    writeSrcDms(s, s1t, s1n, s1sw, dms);
                    WriteString(s, t2);
                    writeSrcDms(s, s2t, s2n, s2sw, dms);
                    WriteString(s, t3);
                    writeSrcDms(s, s3t, s3n, s3sw, dms);
                    WriteString(s, t4);
                });
            }

            void writeInstAction(string dms, Action a)
            {
                WriteString(s, tab);
                int l = dms.Length;
                l = l == 0 ? 4 : l;
                writeDstCC(l, dms, () => {
                    a.Invoke();
                    if (dms != "") WriteString(s, $".{dms}");
                });
            }

            void writeInstInner(string d1ms, string d2ms, Modifier m, string t1, string t2)
            {
                WriteString(s, tab);
                int l = dms.Length;
                l = l == 0 ? 4 : l;
                writeDstCC(l, d1ms, () => {
                    WriteString(s, l != 1 || m == Modifier.INT || m == Modifier.UINT ? getTypeMod(m, l) : "");
                    WriteString(s, $"{t1}(");
                    if (d2ms == null) writeSrc(s, st, sn, ssw);
                    else              writeSrcDms(s, st, sn, ssw, d2ms);
                    WriteString(s, $"){t2}");
                    WriteString(s, l != 1 || m == Modifier.INT || m == Modifier.UINT ? ")" : "");
                });
            }

            void writeInstInner2(string d1ms, string d2ms, Modifier m, string t1, string t2, string t3)
            {
                WriteString(s, tab);
                int l = dms.Length;
                l = l == 0 ? 4 : l;
                writeDstCC(l, d1ms, () => {
                    WriteString(s, l != 1 || m == Modifier.INT || m == Modifier.UINT ? getTypeMod(m, l) : "");
                    WriteString(s, $"{t1}(");
                    if (l < 2 && d2ms == null)
                        writeSrc(s, st, sn, ssw);
                    else if (l < 2)
                        writeSrcDms(s, st, sn, ssw, d2ms);
                    else if (d2ms == null)
                    {
                        writeSrc(s, st, sn, ssw);
                        WriteString(s, $"), {t2}(");
                        writeSrc(s, st, sn, ssw);
                    }
                    else
                    {
                        writeSrcDms(s, st, sn, ssw, d2ms);
                        WriteString(s, $"), {t2}(");
                        writeSrcDms(s, st, sn, ssw, d2ms);
                    }
                    WriteString(s, $")");
                    if (l == 1) WriteString(s, m == Modifier.INT || m == Modifier.UINT ? ")" : "");
                    else if (l == 2) WriteString(s, $")");
                    else if (l == 3) WriteString(s, $", 0)");
                    else if (l == 4) WriteString(s, $", 0, 0)");
                    WriteString(s, t3);
                });
            }

            void writeInstInnerAction(string dms, Modifier m, Action a)
            {
                WriteString(s, tab);
                int l = dms.Length;
                l = l == 0 ? 4 : l;
                writeDstCC(l, dms, () => {
                    WriteString(s, l != 1 || m == Modifier.INT || m == Modifier.UINT
                        ? $"{getTypeMod(m, l)}(" : "");
                    a.Invoke();
                    WriteString(s, l != 1 || m == Modifier.INT || m == Modifier.UINT
                        ? ")" : "");
                });
            }

            static string getTypeMod(Modifier m, int l) =>
                m switch
                {
                    Modifier. INT when l < 2 => "int",
                    Modifier.UINT when l < 2 => "uint",
                    _             when l < 2 => "float",
                    Modifier. INT => $"ivec{l}",
                    Modifier.UINT => $"uvec{l}",
                    _             => $"vec{l}",
                };

            static void writeSrc(Stream s, IType st, string sn, string ssw)
            {
                if (st.Sign != Sign.N)
                    WriteString(s, st.Sign == Sign.P ? "+" : "-");
                if (st.Abs) WriteString(s, "abs(");
                WriteString(s, $"{(ssw == null ? sn : $"{sn}.{ssw}")}");
                if (st.Abs) WriteString(s, ")");
            }

            static void writeSrcDms(Stream s, IType st, string sn, string ssw, string dms)
            {
                if (st.Sign != Sign.N)
                    WriteString(s, st.Sign == Sign.P ? "+" : "-");
                if (st.Abs) WriteString(s, "abs(");
                if (ssw == null)
                    WriteString(s, st.IsVal || dms == "" || dms == "xyzw"? sn : $"{sn}.{dms}");
                else if (dms == "" || dms == "xyzw")
                    WriteString(s, $"{sn}.{ssw}");
                else
                    WriteString(s, $"{sn}.{ssw.Substring(0, dms.Length)}");
                if (st.Abs) WriteString(s, ")");
            }

            static void writeSrcDmsEl(Stream s, IType st, string sn, string ssw, string dms, int el)
            {
                if (st.Sign != Sign.N)
                    WriteString(s, st.Sign == Sign.P ? "+" : "-");
                if (st.Abs) WriteString(s, "abs(");
                if (ssw == null)
                    WriteString(s, st.IsVal || dms == "" ? sn : $"{sn}.{dms[el]}");
                else if (dms == "" || dms == "xyzw")
                    WriteString(s, $"{sn}.{ssw}");
                else
                    WriteString(s, $"{sn}.{ssw.Substring(0, dms.Length)[el]}");
                if (st.Abs) WriteString(s, ")");
            }

            void writeDstCC(int l, string dms, Action a)
            {
                if (dn == "gl_PointSize" || dn == "gl_FragDepth")
                    WriteString(s, $"{dn} = ");
                else
                    WriteString(s, dms == "" || dms == "xyzw" ? $"{dn} = " : $"{dn}.{dms} = ");
                if (hcc)
                {
                    if (l > 1 && cc.Swizzle != null && cc.Swizzle.Length == 1)
                        WriteString(s, getCCOp(cc.Swizzle));
                    else if (l > 1)
                        WriteString(s, getCCOpVec(cc.Swizzle, l, dms));
                    else
                        WriteString(s, getCCOp(cc.Swizzle));
                    WriteString(s, $", ");
                }

                bool clamp = (flags & (InstFlags.S | InstFlags.s)) != 0;
                if (clamp) WriteString(s, "clamp(");
                a.Invoke();
                if (clamp && l == 1)
                    WriteString(s, $", {((flags & InstFlags.S) != 0 ? 0 : -1)}, 1)");
                else if (clamp)
                    WriteString(s, $", vec{l}({((flags & InstFlags.S) != 0 ? 0 : -1)}), vec{l}(1))");

                if (hcc)
                    WriteString(s, $", {(dms == "" || dms == "xyzw" ? dn : $"{dn}.{dms}")})");
            }

            string getCCOp(string ccSwizzle)
            {
                if (ccSwizzle == null || ccSwizzle.Length == 0) ccSwizzle = "x";

                string v = ccOp >= CCOp.EQ1 && ccOp <= CCOp.FL1 ? "cc1" : "cc0";
                string op;
                switch (ccOp)
                {
                    case CCOp.EQ: case CCOp.EQ0: case CCOp.EQ1: op = "EQ"; break;
                    case CCOp.GE: case CCOp.GE0: case CCOp.GE1: op = "GE"; break;
                    case CCOp.GT: case CCOp.GT0: case CCOp.GT1: op = "GT"; break;
                    case CCOp.LE: case CCOp.LE0: case CCOp.LE1: op = "LE"; break;
                    case CCOp.LT: case CCOp.LT0: case CCOp.LT1: op = "LT"; break;
                    case CCOp.NE: case CCOp.NE0: case CCOp.NE1: op = "NE"; break;
                    case CCOp.TR: case CCOp.TR0: case CCOp.TR1: op = "TR"; break;
                    case CCOp.FL: case CCOp.FL0: case CCOp.FL1: op = "FL"; break;
                    default: throw new Exception($"0x006D Invalid CCOp \"{ccOp}\"");
                }
                return $"CC{op}({v}.{ccSwizzle}";
            }

            string getCCOpVec(string ccSwizzle, int l, string dms)
            {
                if (ccSwizzle == null) ccSwizzle = "";
                else if (ccSwizzle.Length == 1) { ccSwizzle = $".{ccSwizzle}"; dms = ""; }
                else if (ccSwizzle.Length == 4) ccSwizzle = $".{ccSwizzle}";

                string v = ccOp >= CCOp.EQ1 && ccOp <= CCOp.FL1 ? "cc1" : "cc0";
                string op;
                switch (ccOp)
                {
                    case CCOp.EQ: case CCOp.EQ0: case CCOp.EQ1: op = "EQ"; break;
                    case CCOp.GE: case CCOp.GE0: case CCOp.GE1: op = "GE"; break;
                    case CCOp.GT: case CCOp.GT0: case CCOp.GT1: op = "GT"; break;
                    case CCOp.LE: case CCOp.LE0: case CCOp.LE1: op = "LE"; break;
                    case CCOp.LT: case CCOp.LT0: case CCOp.LT1: op = "LT"; break;
                    case CCOp.NE: case CCOp.NE0: case CCOp.NE1: op = "NE"; break;
                    case CCOp.TR: case CCOp.TR0: case CCOp.TR1: op = "TR"; break;
                    case CCOp.FL: case CCOp.FL0: case CCOp.FL1: op = "FL"; break;
                    default: throw new Exception($"0x006E Invalid CCOp \"{ccOp}\"");
                }
                return $"CC{op}Vec({v}{ccSwizzle}{(dms == "" || dms == "xyzw" ? "" : $".{dms}")}, {l}";
            }
        }

        private IType GetSrcOperandType(Mode mode, Modifier m, string str, bool abs, Sign sign)
        {
            bool cInit = str.Contains("{") && str.Contains("}");
            if (cInit)
                str = str.Substring(1, str.Length - 2);

            int i1, i2 = -1, i3 = -1;
            IType t;
            if (str.StartsWith("state."))
            {
                str = str.Substring(str.IndexOf(".") + 1);
                if (str.Contains("["))
                {
                    i1 = str.IndexOf('[');
                    i2 = str.IndexOf(']');
                    string d = i2 + 2 > str.Length ? str.Substring(i1 + 1)
                        : str.Substring(i1 + 1, i2 - i1);
                    d = d.Replace("]", "");
                    string e = str.Substring(0, i1);
                    string h = i2 + 2 > str.Length ? "" : str.Substring(i2 + 2);
                    if (!int.TryParse(d, out i2)) i2 = -1;
                    if (d.Contains(".."))
                        throw new Exception("0x0022 Unsupported");

                    i1 = h.IndexOf('[');
                    if (i1 > 0)
                    {
                        i3 = h.IndexOf(']');
                        string f = i3 + 2 > h.Length ? h.Substring(i1 + 1)
                            : h.Substring(i1 + 1, i3 - i1);
                        f = f.Replace("]", "");
                        h = h.Substring(0, i1);
                        if (!int.TryParse(f, out i3)) i3 = -1;
                        if (f.Contains(".."))
                            throw new Exception("0x007C Unsupported");
                    }

                    if (int.TryParse(d, out int v))
                        t = e switch
                        {
                            "matrix.modelview" when i2 != -1 =>
                                t = h switch
                                {
                                    ""          => new TypeIndex(Var.StateMatrixModelViewN        , abs, sign, i2),
                                    "inverse"   => new TypeIndex(Var.StateMatrixInvModelViewN     , abs, sign, i2),
                                    "transpose" => new TypeIndex(Var.StateMatrixTransModelViewN   , abs, sign, i2),
                                    "invtrans"  => new TypeIndex(Var.StateMatrixInvTransModelViewN, abs, sign, i2),
                                    "row" when i3 != -1 =>
                                        new TypeIndexIndex(Var.StateMatrixModelViewNRowO, abs, sign, i2, i3),
                                    _ => throw new Exception("0x005C Unsupported"),
                                },
                            "matrix.texture" when i2 != -1 =>
                                t = h switch
                                {
                                    ""          => new TypeIndex(Var.StateMatrixTextureN        , abs, sign, i2),
                                    "inverse"   => new TypeIndex(Var.StateMatrixInvTextureN     , abs, sign, i2),
                                    "transpose" => new TypeIndex(Var.StateMatrixTransTextureN   , abs, sign, i2),
                                    "invtrans"  => new TypeIndex(Var.StateMatrixInvTransTextureN, abs, sign, i2),
                                    "row" when i3 != -1 =>
                                        new TypeIndexIndex(Var.StateMatrixTextureNRowO, abs, sign, i2, i3),
                                    _ => throw new Exception("0x005D Unsupported"),
                                },
                            "matrix.palette" when i2 != -1 =>
                                t = h switch
                                {
                                    ""          => new TypeIndex(Var.StateMatrixPaletteN        , abs, sign, i2),
                                    "inverse"   => new TypeIndex(Var.StateMatrixInvPaletteN     , abs, sign, i2),
                                    "transpose" => new TypeIndex(Var.StateMatrixTransPaletteN   , abs, sign, i2),
                                    "invtrans"  => new TypeIndex(Var.StateMatrixInvTransPaletteN, abs, sign, i2),
                                    "row" when i3 != -1 =>
                                        new TypeIndexIndex(Var.StateMatrixPaletteNRowO, abs, sign, i2, i3),
                                    _ => throw new Exception("0x005E Unsupported"),
                                },
                            "matrix.program" when i2 != -1 =>
                                t = h switch
                                {
                                    ""          => new TypeIndex(Var.StateMatrixProgramN        , abs, sign, i2),
                                    "inverse"   => new TypeIndex(Var.StateMatrixInvProgramN     , abs, sign, i2),
                                    "transpose" => new TypeIndex(Var.StateMatrixTransProgramN   , abs, sign, i2),
                                    "invtrans"  => new TypeIndex(Var.StateMatrixInvTransProgramN, abs, sign, i2),
                                    "row" when i3 != -1 =>
                                        new TypeIndexIndex(Var.StateMatrixProgramNRowO, abs, sign, i2, i3),
                                    _ => throw new Exception("0x005F Unsupported"),
                                },
                            "matrix.projection.row" when i2 != -1 =>
                                new TypeIndex(Var.StateMatrixProjectionRowO, abs, sign, i2),
                            "matrix.mvp.row"        when i2 != -1 =>
                                new TypeIndex(Var.StateMatrixMVPRowO       , abs, sign, i2),
                            "matrix.modelview.row" when i2 != -1 =>
                                new TypeIndexIndex(Var.StateMatrixModelViewNRowO, abs, sign, 0, i2),
                            "matrix.texture.row"   when i2 != -1 =>
                                new TypeIndexIndex(Var.StateMatrixTextureNRowO  , abs, sign, 0, i2),
                            "matrix.palette.row"   when i2 != -1 =>
                                new TypeIndexIndex(Var.StateMatrixPaletteNRowO  , abs, sign, 0, i2),
                            "matrix.program.row"   when i2 != -1 =>
                                new TypeIndexIndex(Var.StateMatrixProgramNRowO  , abs, sign, 0, i2),
                            "light" when i2 != -1 =>
                                t = h switch
                                {
                                    "ambient"        => new TypeIndex(Var.StateLightNAmbient      , abs, sign, i2),
                                    "diffuse"        => new TypeIndex(Var.StateLightNDiffuse      , abs, sign, i2),
                                    "specular"       => new TypeIndex(Var.StateLightNSpecular     , abs, sign, i2),
                                    "position"       => new TypeIndex(Var.StateLightNPosition     , abs, sign, i2),
                                    "shininess"      => new TypeIndex(Var.StateLightNShininess    , abs, sign, i2),
                                    "spot.direction" => new TypeIndex(Var.StateLightNSpotDirection, abs, sign, i2),
                                    "half"           => new TypeIndex(Var.StateLightNHalf         , abs, sign, i2),
                                    _ => throw new Exception("0x0023 Unsupported"),
                                },
                            "lightprod" when i2 != -1 =>
                                t = h switch
                                {
                                          "ambient"  => new TypeIndex(Var.StateLightProdNAmbient      , abs, sign, i2),
                                          "diffuse"  => new TypeIndex(Var.StateLightProdNDiffuse      , abs, sign, i2),
                                          "specular" => new TypeIndex(Var.StateLightProdNSpecular     , abs, sign, i2),
                                    "front.ambient"  => new TypeIndex(Var.StateLightProdNFrontAmbient , abs, sign, i2),
                                    "front.diffuse"  => new TypeIndex(Var.StateLightProdNFrontDiffuse , abs, sign, i2),
                                    "front.specular" => new TypeIndex(Var.StateLightProdNFrontSpecular, abs, sign, i2),
                                     "back.ambient"  => new TypeIndex(Var.StateLightProdNBackAmbient  , abs, sign, i2),
                                     "back.diffuse"  => new TypeIndex(Var.StateLightProdNBackDiffuse  , abs, sign, i2),
                                     "back.specular" => new TypeIndex(Var.StateLightProdNBackSpecular , abs, sign, i2),
                                    _ => throw new Exception("0x0024 Unsupported"),
                                },
                            "texgen" when i2 != -1 =>
                                t = h switch
                                {
                                       "eye.s" => new TypeIndex(Var.StateTexGenNEyeS   , abs, sign, i2),
                                       "eye.t" => new TypeIndex(Var.StateTexGenNEyeT   , abs, sign, i2),
                                       "eye.r" => new TypeIndex(Var.StateTexGenNEyeR   , abs, sign, i2),
                                       "eye.q" => new TypeIndex(Var.StateTexGenNEyeQ   , abs, sign, i2),
                                    "object.s" => new TypeIndex(Var.StateTexGenNObjectR, abs, sign, i2),
                                    "object.t" => new TypeIndex(Var.StateTexGenNObjectT, abs, sign, i2),
                                    "object.r" => new TypeIndex(Var.StateTexGenNObjectR, abs, sign, i2),
                                    "object.q" => new TypeIndex(Var.StateTexGenNObjectQ, abs, sign, i2),
                                    _ => throw new Exception("0x0025 Unsupported"),
                                },
                            "texenv" when i2 != -1 =>
                                t = h switch
                                {
                                    "color" => new TypeIndex(Var.StateTexEnvNColor, abs, sign, i2),
                                    _ => throw new Exception("0x0026 Unsupported"),
                                },
                            "clip" when i2 != -1 =>
                                t = h switch
                                {
                                    "plane" => new TypeIndex(Var.StateClipNPlane, abs, sign, i2),
                                    _ => throw new Exception("0x0027 Unsupported"),
                                },
                            _ => throw new Exception("0x0028 Unsupported"),
                        };
                    else throw new Exception("0x0029 Unsupported");
                    return t;
                }
                else
                {
                    i1 = str.IndexOf('[');
                    if (i1 > 0)
                    {
                        i2 = str.IndexOf(']');
                        string d = i2 + 2 > str.Length ? str.Substring(i1 + 1)
                            : str.Substring(i1 + 1, i2 - i1);
                        d = d.Replace("]", "");
                        string e = str.Substring(0, i1);
                        string h = i2 + 2 > str.Length ? "" : str.Substring(i2 + 2);
                        if (!int.TryParse(d, out i2)) i2 = -1;
                        if (d.Contains(".."))
                            throw new Exception("0x007D Unsupported");
                    }

                    t = str switch
                    {
                        "material.ambient"            => new Type(Var.StateMaterialAmbient          , abs, sign),
                        "material.diffuse"            => new Type(Var.StateMaterialDiffuse          , abs, sign),
                        "material.specular"           => new Type(Var.StateMaterialSpecular         , abs, sign),
                        "material.emission"           => new Type(Var.StateMaterialEmission         , abs, sign),
                        "material.shininess"          => new Type(Var.StateMaterialShininess        , abs, sign),
                        "material.front.ambient"      => new Type(Var.StateMaterialFrontAmbient     , abs, sign),
                        "material.front.diffuse"      => new Type(Var.StateMaterialFrontDiffuse     , abs, sign),
                        "material.front.specular"     => new Type(Var.StateMaterialFrontSpecular    , abs, sign),
                        "material.front.emission"     => new Type(Var.StateMaterialFrontEmission    , abs, sign),
                        "material.front.shininess"    => new Type(Var.StateMaterialFrontShininess   , abs, sign),
                        "material.back.ambient"       => new Type(Var.StateMaterialBackAmbient      , abs, sign),
                        "material.back.diffuse"       => new Type(Var.StateMaterialBackDiffuse      , abs, sign),
                        "material.back.specular"      => new Type(Var.StateMaterialBackSpecular     , abs, sign),
                        "material.back.emission"      => new Type(Var.StateMaterialBackEmission     , abs, sign),
                        "material.back.shininess"     => new Type(Var.StateMaterialBackShininess    , abs, sign),
                        "lightmodel.ambient"          => new Type(Var.StateLightModelAmbient        , abs, sign),
                        "lightmodel.scenecolor"       => new Type(Var.StateLightModelSceneColor     , abs, sign),
                        "lightmodel.front.scenecolor" => new Type(Var.StateLightModelFrontSceneColor, abs, sign),
                        "lightmodel.back.scenecolor"  => new Type(Var.StateLightModelBackSceneColor , abs, sign),
                        "fog.color"                   => new Type(Var.StateFogColor                 , abs, sign),
                        "fog.params"                  => new Type(Var.StateFogParams                , abs, sign),
                        "point.attenuation"           => new Type(Var.StatePointAttenuation         , abs, sign),
                        "point.size"                  => new Type(Var.StatePointSize                , abs, sign),
                        "depth.range"                 => new Type(Var.StateDepthRange               , abs, sign),
                        "matrix.projection"           => new Type(Var.StateMatrixProjection         , abs, sign),
                        "matrix.mvp"                  => new Type(Var.StateMatrixMVP                , abs, sign),
                        "matrix.projection.inverse"   => new Type(Var.StateMatrixInvProjection      , abs, sign),
                        "matrix.mvp.inverse"          => new Type(Var.StateMatrixInvMVP             , abs, sign),
                        "matrix.projection.transpose" => new Type(Var.StateMatrixTransProjection    , abs, sign),
                        "matrix.mvp.transpose"        => new Type(Var.StateMatrixTransMVP           , abs, sign),
                        "matrix.projection.invtrans"  => new Type(Var.StateMatrixInvTransProjection , abs, sign),
                        "matrix.mvp.invtrans"         => new Type(Var.StateMatrixInvTransMVP        , abs, sign),
                        "matrix.modelview"            => new TypeIndex(Var.StateMatrixModelViewN        , abs, sign, 0),
                        "matrix.texture"              => new TypeIndex(Var.StateMatrixTextureN          , abs, sign, 0),
                        "matrix.palette"              => new TypeIndex(Var.StateMatrixPaletteN          , abs, sign, 0),
                        "matrix.program"              => new TypeIndex(Var.StateMatrixProgramN          , abs, sign, 0),
                        "matrix.modelview.inverse"    => new TypeIndex(Var.StateMatrixInvModelViewN     , abs, sign, 0),
                        "matrix.texture.inverse"      => new TypeIndex(Var.StateMatrixInvTextureN       , abs, sign, 0),
                        "matrix.palette.inverse"      => new TypeIndex(Var.StateMatrixInvPaletteN       , abs, sign, 0),
                        "matrix.program.inverse"      => new TypeIndex(Var.StateMatrixInvProgramN       , abs, sign, 0),
                        "matrix.modelview.transpose"  => new TypeIndex(Var.StateMatrixTransModelViewN   , abs, sign, 0),
                        "matrix.texture.transpose"    => new TypeIndex(Var.StateMatrixTransTextureN     , abs, sign, 0),
                        "matrix.palette.transpose"    => new TypeIndex(Var.StateMatrixTransPaletteN     , abs, sign, 0),
                        "matrix.program.transpose"    => new TypeIndex(Var.StateMatrixTransProgramN     , abs, sign, 0),
                        "matrix.modelview.invtrans"   => new TypeIndex(Var.StateMatrixInvTransModelViewN, abs, sign, 0),
                        "matrix.texture.invtrans"     => new TypeIndex(Var.StateMatrixInvTransTextureN  , abs, sign, 0),
                        "matrix.palette.invtrans"     => new TypeIndex(Var.StateMatrixInvTransPaletteN  , abs, sign, 0),
                        "matrix.program.invtrans"     => new TypeIndex(Var.StateMatrixInvTransProgramN  , abs, sign, 0),
                        _ => throw new Exception("0x002A Unsupported"),
                    };
                }
                return t;
            }

            if (mode == Mode.VertexProgram)
            {
                if (str.Contains("["))
                {
                    t = default;

                    i1 = str.IndexOf('[');
                    i2 = str.IndexOf(']');
                    string d = i2 + 2 > str.Length ? str.Substring(i1 + 1)
                        : str.Substring(i1 + 1, i2 - i1);
                    d = d.Substring(0, d.IndexOf(']'));
                    string e = str.Substring(0, i1);
                    string h = i2 + 1 > str.Length ? "" : str.Substring(i2 + 1);

                    i1 = h.IndexOf('[');
                    if (i1 >= 0)
                    {
                        i3 = h.IndexOf(']');
                        string f = i3 + 2 > h.Length ? h.Substring(i1 + 1)
                            : h.Substring(i1 + 1, i3 - i1);
                        f = f.Substring(0, f.IndexOf(']'));
                        h = h.Substring(0, i1);
                        if (f.Contains(".."))
                        {
                            string[] g = f.Split(new string[] { ".." }, StringSplitOptions.RemoveEmptyEntries);
                            if (g.Length != 2 || !int.TryParse(d, out int v) || !int.TryParse(g[0], out int v0)
                                || !int.TryParse(g[1], out int v1)) throw new Exception("0x007E Unsupported");

                            t = e switch
                            {
                                "program.buffer" => new TypeIndexRange(Var.ProgramBufferNOP, abs, sign, v, v0, v1),
                                _ => throw new Exception($"0x007F Unsupported {e}[{v}][{v0}..{v1}]"),
                            };
                        }
                        else if (int.TryParse(d, out int v0) && int.TryParse(f, out int v1))
                        {
                            t = e switch
                            {
                                "program.buffer" => new TypeIndexIndex(Var.ProgramBufferNO, abs, sign, v0, v1),
                                _ => throw new Exception($"0x0080 Unsupportted {e}[{v0}][{v1}]"),
                            };
                        }
                        else
                            throw new Exception("0x0081 Unsupported");
                    }
                    else if (d.Contains(".."))
                    {
                        string[] g = d.Split(new string[] { ".." }, StringSplitOptions.RemoveEmptyEntries);
                        if (g.Length != 2 || !int.TryParse(g[0], out int v0)
                            || !int.TryParse(g[1], out int v1)) throw new Exception("0x002B Unsupported");

                        t = e switch
                        {
                            "program.env"   => new TypeRange(Var.FragmentProgramEnvNO  , abs, sign, v0, v1),
                            "program.local" => new TypeRange(Var.FragmentProgramLocalNO, abs, sign, v0, v1),
                            _ => throw new Exception($"0x002C Unsupported {e}[{v0}..{v1}]"),
                        };
                    }
                    else if (!int.TryParse(d, out int v))
                    {
                        if (!d.Contains("."))
                            throw new Exception("0x002D Unsupported");

                        string[] g = d.Split('.');
                        d = g[0];
                        if (!temp.Contains(new Temp(Modifier.INT, d)))
                            throw new Exception("0x002E Unsupported");

                        if (g.Length > 1) d = string.Join(".", g);
                        t = new TypeIndexName(Var.Name, abs, sign, e, d);
                    }
                    else
                    {
                        t = e switch
                        {
                            "program.env"        => new TypeIndex(Var.VertexProgramEnvN      , abs, sign, v),
                            "program.local"      => new TypeIndex(Var.VertexProgramLocalN    , abs, sign, v),
                            "program.buffer"     => new TypeIndex(Var.ProgramBufferN         , abs, sign, v),
                            "result.texcoord"    => new TypeIndex(Var.VertexOutputTexCoordN  , abs, sign, v),
                            "vertex.weight"      => //new TypeIndex(Var.VertexInputWeightN     , abs, sign, v),
                            throw new Exception("0x0071 Unsupportted"),
                            "vertex.texcoord"    => new TypeIndex(Var.VertexInputTexCoordN   , abs, sign, v),
                            "vertex.attrib"      => new TypeIndex(Var.VertexInputAttribN     , abs, sign, v),
                            "vertex.matrixindex" => //new TypeIndex(Var.VertexInputMatrixIndexN, abs, sign, v),
                            throw new Exception("0x0072 Unsupportted"),
                            _ => new TypeName(str, abs, sign),
                        };
                    }
                }
                else
                {
                    t = str switch
                    {
                        "result.position"               => new Type(Var.VertexOutputPosition   , abs, sign),
                        "result.color"                  => new Type(Var.VertexOutputColor      , abs, sign),
                        "result.color.primary"          => new Type(Var.VertexOutputColor1     , abs, sign),
                        "result.color.secondary"        => new Type(Var.VertexOutputColor2     , abs, sign),
                        "result.color.front"            => new Type(Var.VertexOutputColorFront , abs, sign),
                        "result.color.front.primary"    => new Type(Var.VertexOutputColorFront1, abs, sign),
                        "result.color.front.secondary"  => new Type(Var.VertexOutputColorFront2, abs, sign),
                        "result.color.back"             => new Type(Var.VertexOutputColorBack  , abs, sign),
                        "result.color.back.primary"     => new Type(Var.VertexOutputColorBack1 , abs, sign),
                        "result.color.back.secondary"   => new Type(Var.VertexOutputColorBack2 , abs, sign),
                        "result.fogcoord"               => new Type(Var.VertexOutputFogCoord   , abs, sign),
                        "result.pointsize"              => new Type(Var.VertexOutputPointSize  , abs, sign),
                        "result.texcoord"               => new Type(Var.VertexOutputTexCoord   , abs, sign),
                        "vertex.position"               => new Type(Var.VertexInputPosition    , abs, sign),
                        "vertex.weight"                 => new Type(Var.VertexInputWeight      , abs, sign),
                        "vertex.normal"                 => new Type(Var.VertexInputNormal      , abs, sign),
                        "vertex.color"                  => new Type(Var.VertexInputColor       , abs, sign),
                        "vertex.color.primary"          => new Type(Var.VertexInputColor1      , abs, sign),
                        "vertex.color.secondary"        => new Type(Var.VertexInputColor2      , abs, sign),
                        "vertex.fogcoord"               => new Type(Var.VertexInputFogCoord    , abs, sign),
                        "vertex.texcoord"               => new Type(Var.VertexInputTexCoord    , abs, sign),
                        "vertex.matrixindex"            => //new Type(Var.VertexInputMatrixIndex ),
                        throw new Exception("0x0073 Unsupportted"),
                        "vertex.attrib"                 => new Type(Var.VertexInputAttrib      , abs, sign),
                        _ => parseString(str, abs, sign, m, cInit),
                    };
                }
            }
            else if (mode == Mode.FragmentProgram)
            {
                if (str.Contains("["))
                {
                    t = default;

                    i1 = str.IndexOf('[');
                    i2 = str.IndexOf(']');
                    string d = i2 + 2 > str.Length ? str.Substring(i1 + 1)
                        : str.Substring(i1 + 1, i2 - i1);
                    d = d.Replace("]", "");
                    string e = str.Substring(0, i1);
                    string h = i2 + 2 > str.Length ? "" : str.Substring(i2 + 2);

                    i1 = h.IndexOf('[');
                    if (i1 >= 0)
                    {
                        i3 = h.IndexOf(']');
                        string f = i3 + 2 > h.Length ? h.Substring(i1 + 1)
                            : h.Substring(i1 + 1, i3 - i1);
                        f = f.Substring(0, f.IndexOf(']'));
                        h = h.Substring(0, i1);
                        if (f.Contains(".."))
                        {
                            string[] g = f.Split(new string[] { ".." }, StringSplitOptions.RemoveEmptyEntries);
                            if (g.Length != 2 || !int.TryParse(d, out int v) || !int.TryParse(g[0], out int v0)
                                || !int.TryParse(g[1], out int v1)) throw new Exception("0x007E Unsupported");

                            t = e switch
                            {
                                "program.buffer" => new TypeIndexRange(Var.ProgramBufferNOP, abs, sign, v, v0, v1),
                                _ => throw new Exception($"0x0082 Unsupported {e}[{v}][{v0}..{v1}]"),
                            };
                        }
                        else if (int.TryParse(d, out int v0) && int.TryParse(f, out int v1))
                        {
                            t = e switch
                            {
                                "program.buffer" => new TypeIndexIndex(Var.ProgramBufferNO, abs, sign, v0, v1),
                                _ => throw new Exception($"0x0083 Unsupportted {e}[{v0}][{v1}]"),
                            };
                        }
                        else
                            throw new Exception("0x0084 Unsupported");
                    }
                    else if (d.Contains(".."))
                    {
                        string[] g = d.Split(new string[] { ".." }, StringSplitOptions.RemoveEmptyEntries);
                        if (g.Length != 2 || !int.TryParse(g[0], out int v0)
                            || !int.TryParse(g[1], out int v1)) throw new Exception("0x002F Unsupported");

                        t = e switch
                        {
                            //"fragment.clip" => new TypeRange(Var.FragmentInputClipNO   , abs, sign, v0, v1),
                            "program.env"   => new TypeRange(Var.FragmentProgramEnvNO  , abs, sign, v0, v1),
                            "program.local" => new TypeRange(Var.FragmentProgramLocalNO, abs, sign, v0, v1),
                            _ => throw new Exception($"0x004B Unsupported {e}[{v0}..{v1}]"),
                        };
                    }
                    else if (!int.TryParse(d, out int v))
                    {
                        if (!d.Contains("."))
                            throw new Exception("0x0030 Unsupported");

                        string[] g = d.Split('.');
                        d = g[0];
                        if (!temp.Contains(new Temp(Modifier.INT, d)))
                            throw new Exception("0x0070 Unsupported");

                        if (g.Length > 1) d = string.Join(".", g);
                        t = new TypeIndexName(Var.Name, abs, sign, e, d);
                    }
                    else
                    {
                        t = e switch
                        {
                            //"fragment.clip"     => new TypeIndex(Var.FragmentInputClipN    , abs, sign, v),
                            "fragment.texcoord" => new TypeIndex(Var.FragmentInputTexCoordN, abs, sign, v),
                            "fragment.attrib"   => new TypeIndex(Var.FragmentInputAttribN  , abs, sign, v),
                            "program.env"       => new TypeIndex(Var.FragmentProgramEnvN   , abs, sign, v),
                            "program.local"     => new TypeIndex(Var.FragmentProgramLocalN , abs, sign, v),
                            "program.buffer"    => new TypeIndex(Var.ProgramBufferN        , abs, sign, v),
                            "result.color"      => new TypeIndex(Var.FragmentOutputColorN  , abs, sign, v),
                            _ => new TypeName(str, abs, sign),
                        };
                    }
                }
                else
                {
                    t = str switch
                    {
                        "fragment.color"           => new Type(Var.FragmentInputColor   , abs, sign),
                        "fragment.color.primary"   => new Type(Var.FragmentInputColor1  , abs, sign),
                        "fragment.color.secondary" => new Type(Var.FragmentInputColor2  , abs, sign),
                        "fragment.texcoord"        => new Type(Var.FragmentInputTexCoord, abs, sign),
                        "fragment.fogcoord"        => new Type(Var.FragmentInputFogCoord, abs, sign),
                        "fragment.position"        => new Type(Var.FragmentInputPosition, abs, sign),
                        "fragment.facing"          => new Type(Var.FragmentInputFacing  , abs, sign),
                        "result.color"             => new Type(Var.FragmentOutputColor  , abs, sign),
                        "result.depth"             => new Type(Var.FragmentOutputDepth  , abs, sign),
                        _ => parseString(str, abs, sign, m, cInit),
                    };
                }
            }
            else throw new Exception("0x0031 Unsupported");
            return t;

            static IType parseString(string str, bool abs, Sign sign, Modifier m, bool cInit)
            {
                int c, i;
                IType t = default;
                if (str.Contains("{"))
                {
                    str = str.Replace(",{", "\x01{").Replace("},", "}\x01");
                    string[] f = str.Split('\x01');
                    c = f.Length;
                    IType[] array = new IType[c];
                    for (i = 0; i < c; i++)
                    {
                        str = f[i];
                        cInit = str.Contains("{") && str.Contains("}");
                        if (cInit)
                            str = str.Substring(1, str.Length - 2);

                             if (str.StartsWith("+")) { sign = Sign.P; str = str.Substring(1); }
                        else if (str.StartsWith("-")) { sign = Sign.M; str = str.Substring(1); }
                        else                            sign = Sign.N;

                        abs = str.Contains("|");
                        if (abs) str = str.Replace("|", "");
                        array[i] = parseString(str, abs, sign, m, cInit);
                    }
                    return new TypeArray(array);
                }

                string[] g = str.Split(',');
                c = g.Length;
                switch (m)
                {
                    case Modifier.  INT:
                        int[] vi = new int[4];
                        for (i = 0; i < c; i++)
                            if (!int.TryParse(g[i], out vi[i]))
                            { t = new TypeName(str, abs, sign); c = 0; break; }
                        t = c switch
                        {
                            0 => t,
                            1 when !cInit => new TypeVec<int>(Var.Vector4, vi[0], vi[0], vi[0], vi[0], abs, sign),
                            1 => new TypeVec<int>(Var.Value  , vi[0],    0 ,    0 ,    1 , abs, sign),
                            2 => new TypeVec<int>(Var.Vector2, vi[0], vi[1],    0 ,    1 , abs, sign),
                            3 => new TypeVec<int>(Var.Vector3, vi[0], vi[1], vi[2],    1 , abs, sign),
                            4 => new TypeVec<int>(Var.Vector4, vi[0], vi[1], vi[2], vi[3], abs, sign),
                            _ => throw new Exception($"0x0032 Unsupported {str}"),
                        };
                        break;
                    case Modifier. UINT:
                        uint[] vu = new uint[4];
                        for (i = 0; i < c; i++)
                            if (!uint.TryParse(g[i], out vu[i]))
                            { t = new TypeName(str, abs, sign); c = 0; break; }
                        t = c switch
                        {
                            0 => t,
                            1 when !cInit => new TypeVec<uint>(Var.Vector4, vu[0], vu[0], vu[0], vu[0], abs, sign),
                            1 => new TypeVec<uint>(Var.Value  , vu[0],    0 ,    0 ,    1 , abs, sign),
                            2 => new TypeVec<uint>(Var.Vector2, vu[0], vu[1],    0 ,    1 , abs, sign),
                            3 => new TypeVec<uint>(Var.Vector3, vu[0], vu[1], vu[2],    1 , abs, sign),
                            4 => new TypeVec<uint>(Var.Vector4, vu[0], vu[1], vu[2], vu[3], abs, sign),
                            _ => throw new Exception($"0x0033 Unsupported {str}"),
                        };
                        break;
                    default:
                        float[] vf = new float[4];
                        for (i = 0; i < c; i++)
                            if (!float.TryParse(g[i].Replace(".", NDS), out vf[i]))
                            { t = new TypeName(str, abs, sign); c = 0; break; }
                        t = c switch
                        {
                            0 => t,
                            1 when !cInit => new TypeVec<float>(Var.Vector4, vf[0], vf[0], vf[0], vf[0], abs, sign),
                            1 => new TypeVec<float>(Var.Value  , vf[0],    0 ,    0 ,    1 , abs, sign),
                            2 => new TypeVec<float>(Var.Vector2, vf[0], vf[1],    0 ,    1 , abs, sign),
                            3 => new TypeVec<float>(Var.Vector3, vf[0], vf[1], vf[2],    1 , abs, sign),
                            4 => new TypeVec<float>(Var.Vector4, vf[0], vf[1], vf[2], vf[3], abs, sign),
                            _ => throw new Exception($"0x0034 Unsupported {str}"),
                        };
                        break;
                }
                return t;
            }
        }

        private static readonly string NDS =
            NumberFormatInfo.CurrentInfo.NumberDecimalSeparator;

        public enum Mode
        {
              VertexProgram,
            FragmentProgram,
        }

        private static string TypeToString(IType data, Modifier m)
        {
            return data switch
            {
                Type      t => VarToString(t.Var),
                TypeIndex t => VarNToString(t.Var, t.ID),
                TypeIndexIndex t => VarNOToString(t.Var, t.ID0, t.ID1),
                TypeIndexName t => $"{t.Name}[{t.Index}]",
                TypeRange t => throw new Exception("0x0035 unk"), //VarNOToString(t.Var, t.IDStart, t.IDEnd),
                TypeName  t => t.Name,
                TypeVec<float> t => t.X == t.Y && t.Y == t.Z && t.Z == t.W
                    ? m switch
                    {
                        Modifier. INT => $"ivec4({( int)t.X })",
                        Modifier.UINT => $"uvec4({(uint)t.X })",
                                    _ =>  $"vec4({  ToS(t.X)})",
                    }
                    : m switch
                    {
                        Modifier. INT => $"ivec4({( int)t.X }, {( int)t.Y }, {( int)t.Z }, {( int)t.W })",
                        Modifier.UINT => $"uvec4({(uint)t.X }, {(uint)t.Y }, {(uint)t.Z }, {(uint)t.W })",
                                    _ =>  $"vec4({  ToS(t.X)}, {  ToS(t.Y)}, {  ToS(t.Z)}, {  ToS(t.W)})",
                    },
                TypeVec<  int> t => t.X == t.Y && t.Y == t.Z && t.Z == t.W
                    ? m switch
                    {
                        Modifier. INT => $"ivec4({      t.X })",
                        Modifier.UINT => $"uvec4({(uint)t.X })",
                                    _ =>  $"vec4({      t.X })",
                    }
                    : m switch
                    {
                        Modifier. INT => $"ivec4({      t.X }, {      t.Y }, {      t.Z }, {      t.W })",
                        Modifier.UINT => $"uvec4({(uint)t.X }, {(uint)t.Y }, {(uint)t.Z }, {(uint)t.W })",
                                    _ =>  $"vec4({      t.X }, {      t.Y }, {      t.Z }, {      t.W })",
                    },
                TypeVec< uint> t => t.X == t.Y && t.Y == t.Z && t.Z == t.W
                    ? m switch
                    {
                        Modifier. INT => $"ivec4({( int)t.X })",
                        Modifier.UINT => $"uvec4({(uint)t.X })",
                                    _ =>  $"vec4({      t.X })",
                    }
                    : m switch
                    {
                        Modifier. INT => $"ivec4({( int)t.X }, {( int)t.Y }, {( int)t.Z }, {( int)t.W })",
                        Modifier.UINT => $"uvec4({      t.X }, {      t.Y }, {      t.Z }, {      t.W })",
                                    _ =>  $"vec4({      t.X }, {      t.Y }, {      t.Z }, {      t.W })",
                    },
                _ => throw new Exception("0x007A Invalid"),
            };
        }

        private static string TypeMaskToString(IType data, string mask, Modifier m)
        {
            if (mask == null) return TypeToString(data, m);

            int l = mask.Length;
            l = l == 0 ? 4 : l;
            return data switch
            {
                Type      t => VarToString(t.Var),
                TypeIndex t => VarNToString(t.Var, t.ID),
                TypeIndexIndex t => VarNOToString(t.Var, t.ID0, t.ID1),
                TypeRange t => throw new Exception("0x0036 unk"), //VarNOToString(t.Var, t.IDStart, t.IDEnd),
                TypeName  t => t.Name,
                TypeVec<float> t when l == 1 =>
                    m switch
                    {
                        Modifier. INT =>       $"{( int)t.X }",
                        Modifier.UINT =>       $"{(uint)t.X }",
                                    _ =>       $"{  ToS(t.X)}",
                    },
                TypeVec<float> t when l == 2 => t.X == t.Y
                    ? m switch
                    {
                        Modifier. INT => $"ivec2({( int)t.X })",
                        Modifier.UINT => $"uvec2({(uint)t.X })",
                                    _ =>  $"vec2({  ToS(t.X)})",
                    }
                    : m switch
                    {
                        Modifier. INT => $"ivec2({( int)t.X }, {( int)t.Y })",
                        Modifier.UINT => $"uvec2({(uint)t.X }, {(uint)t.Y })",
                                    _ =>  $"vec2({  ToS(t.X)}, {  ToS(t.Y)})",
                    },
                TypeVec<float> t when l == 3 => t.X == t.Y && t.Y == t.Z
                    ? m switch
                    {
                        Modifier. INT => $"ivec3({( int)t.X })",
                        Modifier.UINT => $"uvec3({(uint)t.X })",
                                    _ =>  $"vec3({  ToS(t.X)})",
                    }
                    : m switch
                    {
                        Modifier. INT => $"ivec3({( int)t.X }, {( int)t.Y }, {( int)t.Z })",
                        Modifier.UINT => $"uvec3({(uint)t.X }, {(uint)t.Y }, {(uint)t.Z })",
                                    _ =>  $"vec3({  ToS(t.X)}, {  ToS(t.Y)}, {  ToS(t.Z)})",
                    },
                TypeVec<float> t when l == 4 => t.X == t.Y && t.Y == t.Z && t.Z == t.W
                    ? m switch
                    {
                        Modifier. INT => $"ivec4({( int)t.X })",
                        Modifier.UINT => $"uvec4({(uint)t.X })",
                                    _ =>  $"vec4({  ToS(t.X)})",
                    }
                    : m switch
                    {
                        Modifier. INT => $"ivec4({( int)t.X }, {( int)t.Y }, {( int)t.Z }, {( int)t.W })",
                        Modifier.UINT => $"uvec4({(uint)t.X }, {(uint)t.Y }, {(uint)t.Z }, {(uint)t.W })",
                                    _ =>  $"vec4({  ToS(t.X)}, {  ToS(t.Y)}, {  ToS(t.Z)}, {  ToS(t.W)})",
                    },
                TypeVec<  int> t when l == 1 =>
                    m switch
                    {
                        Modifier. INT =>       $"{      t.X }",
                        Modifier.UINT =>       $"{(uint)t.X }",
                                    _ =>       $"{      t.X }",
                    },
                TypeVec<  int> t when l == 2 => t.X == t.Y
                    ? m switch
                    {
                        Modifier. INT => $"ivec2({      t.X })",
                        Modifier.UINT => $"uvec2({(uint)t.X })",
                                    _ =>  $"vec2({      t.X })",
                    }
                    : m switch
                    {
                        Modifier. INT => $"ivec2({      t.X }, {      t.Y })",
                        Modifier.UINT => $"uvec2({(uint)t.X }, {(uint)t.Y })",
                                    _ =>  $"vec2({      t.X }, {      t.Y })",
                    },
                TypeVec<  int> t when l == 3 => t.X == t.Y && t.Y == t.Z
                    ? m switch
                    {
                        Modifier. INT => $"ivec3({      t.X })",
                        Modifier.UINT => $"uvec3({(uint)t.X })",
                                    _ =>  $"vec3({      t.X })",
                    }
                    : m switch
                    {
                        Modifier. INT => $"ivec3({      t.X }, {      t.Y }, {      t.Z })",
                        Modifier.UINT => $"uvec3({(uint)t.X }, {(uint)t.Y }, {(uint)t.Z })",
                                    _ =>  $"vec3({      t.X }, {      t.Y }, {      t.Z })",
                    },
                TypeVec<  int> t when l == 4 => t.X == t.Y && t.Y == t.Z && t.Z == t.W
                    ? m switch
                    {
                        Modifier. INT => $"ivec4({      t.X })",
                        Modifier.UINT => $"uvec4({(uint)t.X })",
                                    _ =>  $"vec4({      t.X })",
                    }
                    : m switch
                    {
                        Modifier. INT => $"ivec4({      t.X }, {      t.Y }, {      t.Z }, {      t.W })",
                        Modifier.UINT => $"uvec4({(uint)t.X }, {(uint)t.Y }, {(uint)t.Z }, {(uint)t.W })",
                                    _ =>  $"vec4({      t.X }, {      t.Y }, {      t.Z }, {      t.W })",
                    },
                TypeVec< uint> t when l == 1 =>
                    m switch
                    {
                        Modifier. INT =>       $"{( int)t.X }",
                        Modifier.UINT =>       $"{      t.X }",
                                    _ =>       $"{      t.X }",
                    },
                TypeVec< uint> t when l == 2 => t.X == t.Y
                    ? m switch
                    {
                        Modifier. INT => $"ivec2({( int)t.X })",
                        Modifier.UINT => $"uvec2({      t.X })",
                                    _ =>  $"vec2({      t.X })",
                    }
                    : m switch
                    {
                        Modifier. INT => $"ivec2({( int)t.X }, {( int)t.Y })",
                        Modifier.UINT => $"uvec2({      t.X }, {      t.Y })",
                                    _ =>  $"vec2({      t.X }, {      t.Y })",
                    },
                TypeVec< uint> t when l == 3 => t.X == t.Y && t.Y == t.Z
                    ? m switch
                    {
                        Modifier. INT => $"ivec3({( int)t.X })",
                        Modifier.UINT => $"uvec3({      t.X })",
                                    _ =>  $"vec3({      t.X })",
                    }
                    : m switch
                    {
                        Modifier. INT => $"ivec3({( int)t.X }, {( int)t.Y }, {( int)t.Z })",
                        Modifier.UINT => $"uvec3({      t.X }, {      t.Y }, {      t.Z })",
                                    _ =>  $"vec3({      t.X }, {      t.Y }, {      t.Z })",
                    },
                TypeVec< uint> t when l == 4 => t.X == t.Y && t.Y == t.Z && t.Z == t.W
                    ? m switch
                    {
                        Modifier. INT => $"ivec4({( int)t.X })",
                        Modifier.UINT => $"uvec4({      t.X })",
                                    _ =>  $"vec4({      t.X })",
                    }
                    : m switch
                    {
                        Modifier. INT => $"ivec4({( int)t.X }, {( int)t.Y }, {( int)t.Z }, {( int)t.W })",
                        Modifier.UINT => $"uvec4({      t.X }, {      t.Y }, {      t.Z }, {      t.W })",
                                    _ =>  $"vec4({      t.X }, {      t.Y }, {      t.Z }, {      t.W })",
                    },
                _ => throw new Exception("0x007B Invalid BUFFER4"),
            };
        }

        private static string ToS(float v) =>
            v.ToString().ToLower().Replace(NDS, ".");

        private static string VarToString(Var v) =>
            v switch
            {
                Var.VertexInputPosition     => "aPos",
                Var.VertexInputWeight       => "aWeight",
                Var.VertexInputNormal       => "aNormal",
                Var.VertexInputColor        => "aColor0",
                Var.VertexInputColor1       => "aColor0",
                Var.VertexInputColor2       => "aColor1",
                Var.VertexInputFogCoord     => "vec4(aFogCoord, 0, 0, 1)",
                Var.VertexInputTexCoord     => "aTexCoord0",
                //Var.VertexInputMatrixIndex  => "aMatrixIndex",
                Var.VertexInputAttrib       => "aAttrib0",

                Var.VertexOutputPosition    => "gl_Position",
                Var.VertexOutputColor       => "fColor[0]",
                Var.VertexOutputColor1      => "fColor[0]",
                Var.VertexOutputColor2      => "fColor[1]",
                Var.VertexOutputColorFront  => "fColorFront[0]",
                Var.VertexOutputColorFront1 => "fColorFront[0]",
                Var.VertexOutputColorFront2 => "fColorFront[1]",
                Var.VertexOutputColorBack   => "fColorBack[0]",
                Var.VertexOutputColorBack1  => "fColorBack[0]",
                Var.VertexOutputColorBack2  => "fColorBack[1]",
                Var.VertexOutputFogCoord    => "fFogCoord",
                Var.VertexOutputPointSize   => "gl_PointSize",
                Var.VertexOutputTexCoord    => "fTexCoord[0]",

                Var.FragmentInputColor      => "fColor[0]",
                Var.FragmentInputColor1     => "fColor[0]",
                Var.FragmentInputColor2     => "fColor[1]",
                Var.FragmentInputTexCoord   => "fTexCoord[0]",
                Var.FragmentInputFogCoord   => "fFogCoord",
                Var.FragmentInputPosition   => "gl_FragCoord",
                Var.FragmentInputFacing     => "frontFacing",

                Var.FragmentOutputColor     => "oColor0",
                Var.FragmentOutputDepth     => "gl_FragDepth",
                
                Var.StateMaterialAmbient           => "State.Material[0].Ambient",
                Var.StateMaterialDiffuse           => "State.Material[0].Diffuse",
                Var.StateMaterialSpecular          => "State.Material[0].Specular",
                Var.StateMaterialEmission          => "State.Material[0].Emission",
                Var.StateMaterialShininess         => "State.Material[0].Shininess",
                Var.StateMaterialFrontAmbient      => "State.Material[0].Ambient",
                Var.StateMaterialFrontDiffuse      => "State.Material[0].Diffuse",
                Var.StateMaterialFrontSpecular     => "State.Material[0].Specular",
                Var.StateMaterialFrontEmission     => "State.Material[0].Emission",
                Var.StateMaterialFrontShininess    => "State.Material[0].Shininess",
                Var.StateMaterialBackAmbient       => "State.Material[1].Ambient",
                Var.StateMaterialBackDiffuse       => "State.Material[1].Diffuse",
                Var.StateMaterialBackSpecular      => "State.Material[1].Specular",
                Var.StateMaterialBackEmission      => "State.Material[1].Emission",
                Var.StateMaterialBackShininess     => "State.Material[1].Shininess",
                Var.StateLightModelAmbient         => "State.LightModel[0].Ambient",
                Var.StateLightModelSceneColor      => "State.LightModel[0].SceneColor",
                Var.StateLightModelFrontSceneColor => "State.LightModel[0].SceneColor",
                Var.StateLightModelBackSceneColor  => "State.LightModel[1].SceneColor",
                Var.StateFogColor                  => "State.Fog.Color",
                Var.StateFogParams                 => "State.Fog.Params",
                Var.StatePointSize                 => "State.Point.Size",
                Var.StatePointAttenuation          => "State.Point.Attenuation",
                Var.StateDepthRange                => "State.Depth.Range",
                Var.StateMatrixProjection          => "State.Matrix.Projection",
                Var.StateMatrixMVP                 => "State.Matrix.MVP",
                Var.StateMatrixInvProjection       => "State.MatrixInv.Projection",
                Var.StateMatrixInvMVP              => "State.MatrixInv.MVP",
                Var.StateMatrixTransProjection     => "State.MatrixTrans.Projection",
                Var.StateMatrixTransMVP            => "State.MatrixTrans.MVP",
                Var.StateMatrixInvTransProjection  => "State.MatrixInvTrans.Projection",
                Var.StateMatrixInvTransMVP         => "State.MatrixInvTrans.MVP",
                _ => throw new Exception("0x0038 unk"),
            };

        private static string VarNToString(Var v, int n) =>
            v switch
            {
                Var.VertexInputWeightN      => $"aWeight{n}",
                Var.VertexInputTexCoordN    => $"aTexCoord{n}",
                Var.VertexInputAttribN      => $"aAttrib{n}",
                //Var.VertexInputMatrixIndexN => $"aMatrixIndex{n}",

                Var.VertexOutputTexCoordN   => $"fTexCoord[{n}]",

                Var.FragmentInputTexCoordN  => $"fTexCoord[{n}]",
                //Var.FragmentInputClipN      => $"fClip[{n}]",
                Var.FragmentInputAttribN    => $"fAttrib[{n}]",

                Var.FragmentOutputColorN    => $"oColor{n}",

                Var.VertexProgramEnvN       => $"PrgVertEnv[{n}]",
                Var.VertexProgramLocalN     => $"PrgVertLocal[{n}]",

                Var.FragmentProgramEnvN     => $"PrgFragEnv[{n}]",
                Var.FragmentProgramLocalN   => $"PrgFragLocal[{n}]",

                Var.ProgramBufferN          => $"Buffer{n}",

                Var.StateLightNAmbient            => $"State.Light[{n}].Ambient",
                Var.StateLightNDiffuse            => $"State.Light[{n}].Diffuse",
                Var.StateLightNSpecular           => $"State.Light[{n}].Specular",
                Var.StateLightNPosition           => $"State.Light[{n}].Position",
                Var.StateLightNShininess          => $"State.Light[{n}].Shininess",
                Var.StateLightNSpotDirection      => $"State.Light[{n}].SpotDirection",
                Var.StateLightNHalf               => $"State.Light[{n}].Half",
                Var.StateLightProdNAmbient        => $"State.LightProd[0][{n}].Ambient",
                Var.StateLightProdNDiffuse        => $"State.LightProd[0][{n}].Diffuse",
                Var.StateLightProdNSpecular       => $"State.LightProd[0][{n}].Specular",
                Var.StateLightProdNFrontAmbient   => $"State.LightProd[0][{n}].Ambient",
                Var.StateLightProdNFrontDiffuse   => $"State.LightProd[0][{n}].Diffuse",
                Var.StateLightProdNFrontSpecular  => $"State.LightProd[0][{n}].Specular",
                Var.StateLightProdNBackAmbient    => $"State.LightProd[1][{n}].Ambient",
                Var.StateLightProdNBackDiffuse    => $"State.LightProd[1][{n}].Diffuse",
                Var.StateLightProdNBackSpecular   => $"State.LightProd[1][{n}].Specular",
                Var.StateTexGenNEyeS              => $"State.TexGen[{n}].EyeS",
                Var.StateTexGenNEyeT              => $"State.TexGen[{n}].EyeT",
                Var.StateTexGenNEyeR              => $"State.TexGen[{n}].EyeR",
                Var.StateTexGenNEyeQ              => $"State.TexGen[{n}].EyeQ",
                Var.StateTexGenNObjectS           => $"State.TexGen[{n}].ObjectS",
                Var.StateTexGenNObjectT           => $"State.TexGen[{n}].ObjectT",
                Var.StateTexGenNObjectR           => $"State.TexGen[{n}].ObjectR",
                Var.StateTexGenNObjectQ           => $"State.TexGen[{n}].ObjectQ",
                Var.StateTexEnvNColor             => $"State.TexEnv[{n}].Color",
                Var.StateClipNPlane               => $"State.Clip[{n}].Plane",
                Var.StateMatrixModelViewN         => $"State.Matrix.ModelView[{n}]",
                Var.StateMatrixTextureN           => $"State.Matrix.Texture[{n}]",
                Var.StateMatrixPaletteN           => $"State.Matrix.Palette[{n}]",
                Var.StateMatrixProgramN           => $"State.Matrix.Program[{n}]",
                Var.StateMatrixInvModelViewN      => $"State.MatrixInv.ModelView[{n}]",
                Var.StateMatrixInvTextureN        => $"State.MatrixInv.Texture[{n}]",
                Var.StateMatrixInvPaletteN        => $"State.MatrixInv.Palette[{n}]",
                Var.StateMatrixInvProgramN        => $"State.MatrixInv.Program[{n}]",
                Var.StateMatrixTransModelViewN    => $"State.MatrixTrans.ModelView[{n}]",
                Var.StateMatrixTransTextureN      => $"State.MatrixTrans.Texture[{n}]",
                Var.StateMatrixTransPaletteN      => $"State.MatrixTrans.Palette[{n}]",
                Var.StateMatrixTransProgramN      => $"State.MatrixTrans.Program[{n}]",
                Var.StateMatrixInvTransModelViewN => $"State.MatrixInvTrans.ModelView[{n}]",
                Var.StateMatrixInvTransTextureN   => $"State.MatrixInvTrans.Texture[{n}]",
                Var.StateMatrixInvTransPaletteN   => $"State.MatrixInvTrans.Palette[{n}]",
                Var.StateMatrixInvTransProgramN   => $"State.MatrixInvTrans.Program[{n}]",
                Var.StateMatrixProjectionRowO     => $"State.Matrix.Projection[{n}]",
                Var.StateMatrixMVPRowO            => $"State.Matrix.MVP[{n}]",
                _ => throw new Exception("0x0039 unk"),
            };

        private static string VarNOToString(Var v, int n, int o) =>
            v switch
            {
                Var.ProgramBufferNO => $"Buffer{n}[{o}]",

                Var.StateMatrixModelViewNRowO        => $"State.Matrix.ModelView[{n}][{o}]",
                Var.StateMatrixTextureNRowO           => $"State.Matrix.Texture[{n}][{o}]",
                Var.StateMatrixPaletteNRowO           => $"State.Matrix.Palette[{n}][{o}]",
                Var.StateMatrixProgramNRowO           => $"State.Matrix.Program[{n}][{o}]",
                _ => throw new Exception("0x0060 unk"),
            };

        private static void WriteString(Stream s, string str)
        {
            byte[] buf = Encoding.GetBytes(str);
            s.Write(buf, 0, buf.Length);
        }

        #region Operands
        public enum CCOp
        {
            None,
            EQ,
            GE,
            GT,
            LE,
            LT,
            NE,
            TR,
            FL,
            EQ0,
            GE0,
            GT0,
            LE0,
            LT0,
            NE0,
            TR0,
            FL0,
            EQ1,
            GE1,
            GT1,
            LE1,
            LT1,
            NE1,
            TR1,
            FL1,
        }

        public struct CC
        {
            private CCOp op;
            private string swizzle;

            public CCOp Op => op;
            public string Swizzle => swizzle;

            public CC(ref string str, bool trigger = false)
            {
                op = CCOp.None;
                swizzle = null;
                int i1 = str.IndexOf("(");
                int i2 = str.IndexOf(")");
                if (i1 < 0 || i2 < 0)
                {
                    if (trigger)
                        return;
                    else
                        throw new Exception($"0x0064 Invalid CC rule");
                }

                string cct = str.Substring(i1 + 1, i2 - i1 - 1).Replace(" ", "");
                str = str.Substring(0, i1).Replace(" ", "");

                if (NewCC(cct, trigger))
                {
                    op = CCOp.None;
                    swizzle = null;
                }
            }

            public CC(string cct, bool trigger = false)
            {
                op = CCOp.None;
                swizzle = null;

                if (NewCC(cct, trigger))
                {
                    op = CCOp.None;
                    swizzle = null;
                }
            }

            private bool NewCC(string cct, bool trigger)
            {
                string[] s = cct.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                string ccSwizzle = s.Length > 1 ? s[s.Length - 1] : null;
                if (s.Length < 1)
                {
                    if (trigger)
                        return true;
                    else
                        throw new Exception($"0x0067 Invalid CC rule");
                }

                if (ccSwizzle != null)
                {
                    if (ccSwizzle.Length != 1 && ccSwizzle.Length != 4)
                    {
                        if (trigger)
                            return true;
                        else
                            throw new Exception($"0x0068 Invalid CC swizzle \"{ccSwizzle}\"");
                    }

                    ccSwizzle = s[s.Length - 1];
                    int i0;
                    i0 = ccSwizzle.Length;
                    switch (ccSwizzle[0])
                    {
                        case 'x': case 'y': case 'z': case 'w':
                            for (int i = 1; i < i0; i++)
                                switch (ccSwizzle[i])
                                {
                                    case 'x': case 'y': case 'z': case 'w': break;
                                    default:
                                        if (trigger)
                                            return true;
                                        else
                                            throw new Exception($"0x0069 Invalid CC swizzle \"{ccSwizzle}\"");
                                }
                            break;
                        default:
                            if (trigger)
                                return true;
                            else
                                throw new Exception($"0x006A Invalid CC swizzle \"{ccSwizzle}\"");
                    }
                    cct = cct.Substring(0, cct.Length - ccSwizzle.Length - 1);
                    if (ccSwizzle == "xyzw") ccSwizzle = null;
                }

                if (!Enum.TryParse(cct, out CCOp ccOp))
                    if (trigger)
                        return true;
                    else
                        throw new Exception($"0x006B Invalid CC rule \"{cct}\"");

                op = ccOp;
                swizzle = ccSwizzle;
                return false;
            }

            public CC(CCOp op, string swizzle)
            {
                this.op = op;
                this.swizzle = swizzle;
            }

            public override string ToString() =>
                op != CCOp.None ? $" ({op}{(swizzle != null ? $".{swizzle}" : "")})" : "";

            public string ToString(bool brackets) =>
                brackets
                ? (op != CCOp.None ? $" ({op}{(swizzle != null ? $".{swizzle}" : "")})" : "")
                : (op != CCOp.None ? $" {op}{(swizzle != null ? $".{swizzle}" : "")}" : "");
        }

        public struct SrcOperand
        {
            private Modifier m;
            private IType data;
            private string swizzle;

            public IType Data => data;
            public string Swizzle => swizzle == null ? null :
                (swizzle.Length == 1 ? $"{swizzle}{swizzle}{swizzle}{swizzle}" : swizzle);

            public SrcOperand(Var var)
            {
                m = Modifier.FLOAT;
                data = new Type(var, false, Sign.N);
                swizzle = null;
            }

            public SrcOperand(ARBConverter arb, Mode mode, Modifier m, string str)
            {
                this.m = m;
                str = str.Replace(" ", "");
                Sign sign;
                     if (str.StartsWith("+")) { sign = Sign.P; str = str.Substring(1); }
                else if (str.StartsWith("-")) { sign = Sign.M; str = str.Substring(1); }
                else                            sign = Sign.N;

                bool abs = str.Contains("|");
                if (abs) str = str.Replace("|", "");
                data = default;
                swizzle = null;
                string[] s = str.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                swizzle = s.Length > 1 ? s[s.Length - 1] : null;
                if (swizzle != null && (swizzle.Length == 1 || swizzle.Length == 4))
                {
                    int i, i0;
                    i0 = swizzle.Length;
                    switch (swizzle[0])
                    {
                        case 'x': case 'y': case 'z': case 'w':
                            for (i = 1; i < i0; i++)
                                switch (swizzle[i])
                                {
                                    case 'x': case 'y': case 'z': case 'w': break;
                                    default:
                                        data = arb.GetSrcOperandType(mode, m, str, abs, sign);
                                        swizzle = null;
                                        return;
                                }
                            break;
                        case 'r': case 'g': case 'b': case 'a':
                            for (i = 1; i < i0; i++)
                                switch (swizzle[i])
                                {
                                    case 'r': case 'g': case 'b': case 'a': break;
                                    default:
                                        data = arb.GetSrcOperandType(mode, m, str, abs, sign);
                                        swizzle = null;
                                        return;
                                }
                            break;
                        default:
                            swizzle = null;
                            data = arb.GetSrcOperandType(mode, m, str, abs, sign);
                            return;
                    }
                    str = str.Substring(0, str.Length - swizzle.Length - 1);
                    if (i0 == 1) swizzle = $"{swizzle}{swizzle}{swizzle}{swizzle}";
                    if (swizzle == "xyzw" || swizzle == "rgba") swizzle = null;
                    data = arb.GetSrcOperandType(mode, m, str, abs, sign);
                }
                else { data = arb.GetSrcOperandType(mode, m, str, abs, sign); swizzle = null; }
            }

            public override string ToString() =>
                  (data.Abs ? "|" : "")
                + TypeToString(data, m) + (swizzle != null ?  $".{swizzle}" : "")
                + (data.Abs ? "|" : "");
        }

        public struct DstOperand
        {
            private Modifier m;
            private IType data;
            private string mask;
            private CC cc;

            public IType Data => data;
            public string Mask => mask;
            public CC CC => cc;

            public DstOperand(Var var)
            {
                m = Modifier.FLOAT;
                cc = default;
                data = new Type(var, false, Sign.N);
                mask = "";
            }

            public DstOperand(ARBConverter arb, Mode mode, Modifier m, string str)
            {
                this.m = m;
                str = str.Replace(" ", "");
                cc = default;
                string[] s;
                if (str.Contains("("))
                    cc = new CC(ref str);

                s = str.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                mask = s.Length > 1 ? s[s.Length - 1] : "";
                switch (mask)
                {
                    case null: data = arb.GetSrcOperandType(mode, m, str, false, Sign.N); mask = ""; break;
                    case  "x": case  "y": case  "z": case  "w":
                               case "xy": case "xz": case "xw":
                                          case "yz": case "yw":
                                                     case "zw":
                    case  "r": case  "g": case  "b": case  "a":
                               case "rg": case "rb": case "ra":
                                          case "gb": case "ga":
                                                     case "ba":
                    case "xyz" : case "xyw": case "xzw": case "yzw":
                    case "rgb" : case "rga": case "rba": case "gba":
                        str = str.Substring(0, str.Length - mask.Length - 1);
                        data = arb.GetSrcOperandType(mode, m, str, false, Sign.N);
                        break;
                    case "xyzw": case "rgba":
                        str = str.Substring(0, str.Length - mask.Length - 1);
                        data = arb.GetSrcOperandType(mode, m, str, false, Sign.N);
                        mask = "";
                        break;
                    default:
                        mask = "";
                        data = arb.GetSrcOperandType(mode, m, str, false, Sign.N);
                        break;
                }
            }

            public override string ToString() =>
                $"{TypeToString(data, m)}{(mask != "" ? $".{mask}" : "")}{cc}";
        }
        #endregion

        #region Instructions
        public interface IInstruction
        {
            string Name { get; }
            DstOperand DOp { get; }
            InstFlags Flags { get; }
        }

        public static string InstToString(string name, InstFlags flags)
        {
            string s = name;
            if ((flags & InstFlags.R) != 0)
                s += "R";
            else if ((flags & InstFlags.H) != 0)
                s += "H";
            else if ((flags & InstFlags.X) != 0)
                s += "X";
            if ((flags & InstFlags.C) != 0 && (flags & (InstFlags.CC | InstFlags.CC0 | InstFlags.CC1)) == 0)
                s += "C";
            if ((flags & InstFlags.S) != 0)
                s += "_SAT";
            else if ((flags & InstFlags.s) != 0)
                s += "_SSAT";
            if ((flags & InstFlags.F) != 0)
                s += ".F";
            else if ((flags & InstFlags.I) != 0)
                s += ".S";
            else if ((flags & InstFlags.U) != 0)
                s += ".U";
            if ((flags & InstFlags.C) == 0 || ((flags & InstFlags.C) != 0
                && (flags & (InstFlags.CC | InstFlags.CC0 | InstFlags.CC1)) != 0))
            {
                if ((flags & InstFlags.CC) != 0)
                    s += ".CC";
                else if ((flags & InstFlags.CC0) != 0)
                    s += ".CC0";
                else if ((flags & InstFlags.CC1) != 0)
                    s += ".CC1";
            }
            return s;
        }

        #region VecInst
        public struct ABS: IInstruction
        {
            private SrcOperand sop;
            private DstOperand dop;

            public string Name => "ABS";
            public SrcOperand SOp => sop;
            public DstOperand DOp => dop;

            private InstFlags flags;
            public InstFlags Flags => flags;

            public ABS(ARBConverter arb, string[] op, Mode mode, Modifier m, InstFlags flags)
            {
                this.flags = flags;
                sop = new SrcOperand(arb, mode, m, op[1]);
                dop = new DstOperand(arb, mode, m, op[0]);
            }

            public override string ToString() =>
                $"{InstToString(Name, Flags)} {dop}, {sop}";
        }

        public struct ADD : IInstruction
        {
            private SrcOperand sop1;
            private SrcOperand sop2;
            private DstOperand dop;

            public string Name => "ADD";
            public SrcOperand SOp1 => sop1;
            public SrcOperand SOp2 => sop2;
            public DstOperand DOp  => dop;

            private InstFlags flags;
            public InstFlags Flags => flags;

            public ADD(ARBConverter arb, string[] op, Mode mode, Modifier m, InstFlags flags)
            {
                this.flags = flags;
                sop1 = new SrcOperand(arb, mode, m, op[1]);
                sop2 = new SrcOperand(arb, mode, m, op[2]);
                dop  = new DstOperand(arb, mode, m, op[0]);
            }

            public override string ToString() =>
                $"{InstToString(Name, Flags)} {dop}, {sop1}, {sop2}";
        }

        public struct ARL : IInstruction
        {
            private SrcOperand sop;
            private DstOperand dop;

            public string Name => "ARL";
            public SrcOperand SOp => sop;
            public DstOperand DOp => dop;

            private InstFlags flags;
            public InstFlags Flags => flags;

            public ARL(ARBConverter arb, string[] op, Mode mode, Modifier m, InstFlags flags)
            {
                this.flags = flags;
                sop = new SrcOperand(arb, mode, m, op[1]);
                dop = new DstOperand(arb, mode, m, op[0]);
            }

            public override string ToString() =>
                $"{InstToString(Name, Flags)} {dop}, {sop}";
        }

        public struct ARR : IInstruction
        {
            private SrcOperand sop;
            private DstOperand dop;

            public string Name => "ARR";
            public SrcOperand SOp => sop;
            public DstOperand DOp => dop;

            private InstFlags flags;
            public InstFlags Flags => flags;

            public ARR(ARBConverter arb, string[] op, Mode mode, Modifier m, InstFlags flags)
            {
                this.flags = flags;
                sop = new SrcOperand(arb, mode, m, op[1]);
                dop = new DstOperand(arb, mode, m, op[0]);
            }

            public override string ToString() =>
                $"{InstToString(Name, Flags)} {dop}, {sop}";
        }

        public struct CEIL : IInstruction
        {
            private SrcOperand sop;
            private DstOperand dop;

            public string Name => "CEIL";
            public SrcOperand SOp => sop;
            public DstOperand DOp => dop;

            private InstFlags flags;
            public InstFlags Flags => flags;

            public CEIL(ARBConverter arb, string[] op, Mode mode, Modifier m, InstFlags flags)
            {
                this.flags = flags;
                sop = new SrcOperand(arb, mode, m, op[1]);
                dop = new DstOperand(arb, mode, m, op[0]);
            }

            public override string ToString() =>
                $"{InstToString(Name, Flags)} {dop}, {sop}";
        }

        public struct CMP : IInstruction
        {
            private SrcOperand sop1;
            private SrcOperand sop2;
            private SrcOperand sop3;
            private DstOperand dop;

            public string Name => "CMP";
            public SrcOperand SOp1 => sop1;
            public SrcOperand SOp2 => sop2;
            public SrcOperand SOp3 => sop3;
            public DstOperand DOp  => dop;

            private InstFlags flags;
            public InstFlags Flags => flags;

            public CMP(ARBConverter arb, string[] op, Mode mode, Modifier m, InstFlags flags)
            {
                this.flags = flags;
                sop1 = new SrcOperand(arb, mode, m, op[1]);
                sop2 = new SrcOperand(arb, mode, m, op[2]);
                sop3 = new SrcOperand(arb, mode, m, op[3]);
                dop  = new DstOperand(arb, mode, m, op[0]);
            }

            public override string ToString() =>
                $"{InstToString(Name, Flags)} {dop}, {sop1}, {sop2}, {sop3}";
        }

        public struct COS : IInstruction
        {
            private SrcOperand sop;
            private DstOperand dop;

            public string Name => "COS";
            public SrcOperand SOp => sop;
            public DstOperand DOp => dop;

            private InstFlags flags;
            public InstFlags Flags => flags;

            public COS(ARBConverter arb, string[] op, Mode mode, Modifier m, InstFlags flags)
            {
                this.flags = flags;
                sop = new SrcOperand(arb, mode, m, op[1]);
                dop = new DstOperand(arb, mode, m, op[0]);
            }

            public override string ToString() =>
                $"{InstToString(Name, Flags)} {dop}, {sop}";
        }

        public struct DDX : IInstruction
        {
            private SrcOperand sop;
            private DstOperand dop;

            public string Name => "DDX";
            public SrcOperand SOp => sop;
            public DstOperand DOp => dop;

            private InstFlags flags;
            public InstFlags Flags => flags;

            public DDX(ARBConverter arb, string[] op, Mode mode, Modifier m, InstFlags flags)
            {
                this.flags = flags;
                sop = new SrcOperand(arb, mode, m, op[1]);
                dop = new DstOperand(arb, mode, m, op[0]);
            }

            public override string ToString() =>
                $"{InstToString(Name, Flags)} {dop}, {sop}";
        }

        public struct DDY : IInstruction
        {
            private SrcOperand sop;
            private DstOperand dop;

            public string Name => "DDY";
            public SrcOperand SOp => sop;
            public DstOperand DOp => dop;

            private InstFlags flags;
            public InstFlags Flags => flags;

            public DDY(ARBConverter arb, string[] op, Mode mode, Modifier m, InstFlags flags)
            {
                this.flags = flags;
                sop = new SrcOperand(arb, mode, m, op[1]);
                dop = new DstOperand(arb, mode, m, op[0]);
            }

            public override string ToString() =>
                $"{InstToString(Name, Flags)} {dop}, {sop}";
        }

        public struct DIV : IInstruction
        {
            private SrcOperand sop1;
            private SrcOperand sop2;
            private DstOperand dop;

            public string Name => "DIV";
            public SrcOperand SOp1 => sop1;
            public SrcOperand SOp2 => sop2;
            public DstOperand DOp  => dop;

            private InstFlags flags;
            public InstFlags Flags => flags;

            public DIV(ARBConverter arb, string[] op, Mode mode, Modifier m, InstFlags flags)
            {
                this.flags = flags;
                sop1 = new SrcOperand(arb, mode, m, op[1]);
                sop2 = new SrcOperand(arb, mode, m, op[2]);
                dop  = new DstOperand(arb, mode, m, op[0]);
            }

            public override string ToString() =>
                $"{InstToString(Name, Flags)} {dop}, {sop1}, {sop2}";
        }

        public struct DP2 : IInstruction
        {
            private SrcOperand sop1;
            private SrcOperand sop2;
            private DstOperand dop;

            public string Name => "DP2";
            public SrcOperand SOp1 => sop1;
            public SrcOperand SOp2 => sop2;
            public DstOperand DOp  => dop;

            private InstFlags flags;
            public InstFlags Flags => flags;

            public DP2(ARBConverter arb, string[] op, Mode mode, Modifier m, InstFlags flags)
            {
                this.flags = flags;
                sop1 = new SrcOperand(arb, mode, m, op[1]);
                sop2 = new SrcOperand(arb, mode, m, op[2]);
                dop  = new DstOperand(arb, mode, m, op[0]);
            }

            public override string ToString() =>
                $"{InstToString(Name, Flags)} {dop}, {sop1}, {sop2}";
        }

        public struct DP2A : IInstruction
        {
            private SrcOperand sop1;
            private SrcOperand sop2;
            private SrcOperand sop3;
            private DstOperand dop;

            public string Name => "DP2A";
            public SrcOperand SOp1 => sop1;
            public SrcOperand SOp2 => sop2;
            public SrcOperand SOp3 => sop3;
            public DstOperand DOp  => dop;

            private InstFlags flags;
            public InstFlags Flags => flags;

            public DP2A(ARBConverter arb, string[] op, Mode mode, Modifier m, InstFlags flags)
            {
                this.flags = flags;
                sop1 = new SrcOperand(arb, mode, m, op[1]);
                sop2 = new SrcOperand(arb, mode, m, op[2]);
                sop3 = new SrcOperand(arb, mode, m, op[3]);
                dop  = new DstOperand(arb, mode, m, op[0]);
            }

            public override string ToString() =>
                $"{InstToString(Name, Flags)} {dop}, {sop1}, {sop2}";
        }

        public struct DP3 : IInstruction
        {
            private SrcOperand sop1;
            private SrcOperand sop2;
            private DstOperand dop;

            public string Name => "DP3";
            public SrcOperand SOp1 => sop1;
            public SrcOperand SOp2 => sop2;
            public DstOperand DOp  => dop;

            private InstFlags flags;
            public InstFlags Flags => flags;

            public DP3(ARBConverter arb, string[] op, Mode mode, Modifier m, InstFlags flags)
            {
                this.flags = flags;
                sop1 = new SrcOperand(arb, mode, m, op[1]);
                sop2 = new SrcOperand(arb, mode, m, op[2]);
                dop  = new DstOperand(arb, mode, m, op[0]);
            }

            public override string ToString() =>
                $"{InstToString(Name, Flags)} {dop}, {sop1}, {sop2}";
        }

        public struct DP4 : IInstruction
        {
            private SrcOperand sop1;
            private SrcOperand sop2;
            private DstOperand dop;

            public string Name => "DP4";
            public SrcOperand SOp1 => sop1;
            public SrcOperand SOp2 => sop2;
            public DstOperand DOp  => dop;

            private InstFlags flags;
            public InstFlags Flags => flags;

            public DP4(ARBConverter arb, string[] op, Mode mode, Modifier m, InstFlags flags)
            {
                this.flags = flags;
                sop1 = new SrcOperand(arb, mode, m, op[1]);
                sop2 = new SrcOperand(arb, mode, m, op[2]);
                dop  = new DstOperand(arb, mode, m, op[0]);
            }

            public override string ToString() =>
                $"{InstToString(Name, Flags)} {dop}, {sop1}, {sop2}";
        }

        public struct DPH : IInstruction
        {
            private SrcOperand sop1;
            private SrcOperand sop2;
            private DstOperand dop;

            public string Name => "DPH";
            public SrcOperand SOp1 => sop1;
            public SrcOperand SOp2 => sop2;
            public DstOperand DOp  => dop;

            private InstFlags flags;
            public InstFlags Flags => flags;

            public DPH(ARBConverter arb, string[] op, Mode mode, Modifier m, InstFlags flags)
            {
                this.flags = flags;
                sop1 = new SrcOperand(arb, mode, m, op[1]);
                sop2 = new SrcOperand(arb, mode, m, op[2]);
                dop  = new DstOperand(arb, mode, m, op[0]);
            }

            public override string ToString() =>
                $"{InstToString(Name, Flags)} {dop}, {sop1}, {sop2}";
        }

        public struct DST : IInstruction
        {
            private SrcOperand sop1;
            private SrcOperand sop2;
            private DstOperand dop;

            public string Name => "DST";
            public SrcOperand SOp1 => sop1;
            public SrcOperand SOp2 => sop2;
            public DstOperand DOp  => dop;

            private InstFlags flags;
            public InstFlags Flags => flags;

            public DST(ARBConverter arb, string[] op, Mode mode, Modifier m, InstFlags flags)
            {
                this.flags = flags;
                sop1 = new SrcOperand(arb, mode, m, op[1]);
                sop2 = new SrcOperand(arb, mode, m, op[2]);
                dop  = new DstOperand(arb, mode, m, op[0]);
            }

            public override string ToString() =>
                $"{InstToString(Name, Flags)} {dop}, {sop1}, {sop2}";
        }

        public struct EX2 : IInstruction
        {
            private SrcOperand sop;
            private DstOperand dop;

            public string Name => "EX2";
            public SrcOperand SOp => sop;
            public DstOperand DOp => dop;

            private InstFlags flags;
            public InstFlags Flags => flags;

            public EX2(ARBConverter arb, string[] op, Mode mode, Modifier m, InstFlags flags)
            {
                this.flags = flags;
                sop = new SrcOperand(arb, mode, m, op[1]);
                dop = new DstOperand(arb, mode, m, op[0]);
            }

            public override string ToString() =>
                $"{InstToString(Name, Flags)} {dop}, {sop}";
        }

        public struct EXP : IInstruction
        {
            private SrcOperand sop;
            private DstOperand dop;

            public string Name => "EXP";
            public SrcOperand SOp => sop;
            public DstOperand DOp => dop;

            private InstFlags flags;
            public InstFlags Flags => flags;

            public EXP(ARBConverter arb, string[] op, Mode mode, Modifier m, InstFlags flags)
            {
                this.flags = flags;
                sop = new SrcOperand(arb, mode, m, op[1]);
                dop = new DstOperand(arb, mode, m, op[0]);
            }

            public override string ToString() =>
                $"{InstToString(Name, Flags)} {dop}, {sop}";
        }

        public struct FLR : IInstruction
        {
            private SrcOperand sop;
            private DstOperand dop;

            public string Name => "FLR";
            public SrcOperand SOp => sop;
            public DstOperand DOp => dop;

            private InstFlags flags;
            public InstFlags Flags => flags;

            public FLR(ARBConverter arb, string[] op, Mode mode, Modifier m, InstFlags flags)
            {
                this.flags = flags;
                sop = new SrcOperand(arb, mode, m, op[1]);
                dop = new DstOperand(arb, mode, m, op[0]);
            }

            public override string ToString() =>
                $"{InstToString(Name, Flags)} {dop}, {sop}";
        }

        public struct FRC : IInstruction
        {
            private SrcOperand sop;
            private DstOperand dop;

            public string Name => "FRC";
            public SrcOperand SOp => sop;
            public DstOperand DOp => dop;

            private InstFlags flags;
            public InstFlags Flags => flags;

            public FRC(ARBConverter arb, string[] op, Mode mode, Modifier m, InstFlags flags)
            {
                this.flags = flags;
                sop = new SrcOperand(arb, mode, m, op[1]);
                dop = new DstOperand(arb, mode, m, op[0]);
            }

            public override string ToString() =>
                $"{InstToString(Name, Flags)} {dop}, {sop}";
        }

        public struct LG2 : IInstruction
        {
            private SrcOperand sop;
            private DstOperand dop;

            public string Name => "LG2";
            public SrcOperand SOp => sop;
            public DstOperand DOp => dop;

            private InstFlags flags;
            public InstFlags Flags => flags;

            public LG2(ARBConverter arb, string[] op, Mode mode, Modifier m, InstFlags flags)
            {
                this.flags = flags;
                sop = new SrcOperand(arb, mode, m, op[1]);
                dop = new DstOperand(arb, mode, m, op[0]);
            }

            public override string ToString() =>
                $"{InstToString(Name, Flags)} {dop}, {sop}";
        }

        public struct LIT : IInstruction
        {
            private SrcOperand sop;
            private DstOperand dop;

            public string Name => "LIT";
            public SrcOperand SOp => sop;
            public DstOperand DOp => dop;

            private InstFlags flags;
            public InstFlags Flags => flags;

            public LIT(ARBConverter arb, string[] op, Mode mode, Modifier m, InstFlags flags)
            {
                this.flags = flags;
                sop = new SrcOperand(arb, mode, m, op[1]);
                dop = new DstOperand(arb, mode, m, op[0]);
            }

            public override string ToString() =>
                $"{InstToString(Name, Flags)} {dop}, {sop}";
        }

        public struct LOG : IInstruction
        {
            private SrcOperand sop;
            private DstOperand dop;

            public string Name => "LOG";
            public SrcOperand SOp => sop;
            public DstOperand DOp => dop;

            private InstFlags flags;
            public InstFlags Flags => flags;

            public LOG(ARBConverter arb, string[] op, Mode mode, Modifier m, InstFlags flags)
            {
                this.flags = flags;
                sop = new SrcOperand(arb, mode, m, op[1]);
                dop = new DstOperand(arb, mode, m, op[0]);
            }

            public override string ToString() =>
                $"{InstToString(Name, Flags)} {dop}, {sop}";
        }

        public struct LRP : IInstruction
        {
            private SrcOperand sop1;
            private SrcOperand sop2;
            private SrcOperand sop3;
            private DstOperand dop;

            public string Name => "LRP";
            public SrcOperand SOp1 => sop1;
            public SrcOperand SOp2 => sop2;
            public SrcOperand SOp3 => sop3;
            public DstOperand DOp  => dop;

            private InstFlags flags;
            public InstFlags Flags => flags;

            public LRP(ARBConverter arb, string[] op, Mode mode, Modifier m, InstFlags flags)
            {
                this.flags = flags;
                sop1 = new SrcOperand(arb, mode, m, op[1]);
                sop2 = new SrcOperand(arb, mode, m, op[2]);
                sop3 = new SrcOperand(arb, mode, m, op[3]);
                dop  = new DstOperand(arb, mode, m, op[0]);
            }

            public override string ToString() =>
                $"{InstToString(Name, Flags)} {dop}, {sop1}, {sop2}, {sop3}";
        }

        public struct MAD : IInstruction
        {
            private SrcOperand sop1;
            private SrcOperand sop2;
            private SrcOperand sop3;
            private DstOperand dop;

            public string Name => "MAD";
            public SrcOperand SOp1 => sop1;
            public SrcOperand SOp2 => sop2;
            public SrcOperand SOp3 => sop3;
            public DstOperand DOp  => dop;

            private InstFlags flags;
            public InstFlags Flags => flags;

            public MAD(ARBConverter arb, string[] op, Mode mode, Modifier m, InstFlags flags)
            {
                this.flags = flags;
                sop1 = new SrcOperand(arb, mode, m, op[1]);
                sop2 = new SrcOperand(arb, mode, m, op[2]);
                sop3 = new SrcOperand(arb, mode, m, op[3]);
                dop  = new DstOperand(arb, mode, m, op[0]);
            }

            public override string ToString() =>
                $"{InstToString(Name, Flags)} {dop}, {sop1}, {sop2}, {sop3}";
        }

        public struct MAX : IInstruction
        {
            private SrcOperand sop1;
            private SrcOperand sop2;
            private DstOperand dop;

            public string Name => "MAX";
            public SrcOperand SOp1 => sop1;
            public SrcOperand SOp2 => sop2;
            public DstOperand DOp  => dop;

            private InstFlags flags;
            public InstFlags Flags => flags;

            public MAX(ARBConverter arb, string[] op, Mode mode, Modifier m, InstFlags flags)
            {
                this.flags = flags;
                sop1 = new SrcOperand(arb, mode, m, op[1]);
                sop2 = new SrcOperand(arb, mode, m, op[2]);
                dop  = new DstOperand(arb, mode, m, op[0]);
            }

            public override string ToString() =>
                $"{InstToString(Name, Flags)} {dop}, {sop1}, {sop2}";
        }

        public struct MIN : IInstruction
        {
            private SrcOperand sop1;
            private SrcOperand sop2;
            private DstOperand dop;

            public string Name => "MIN";
            public SrcOperand SOp1 => sop1;
            public SrcOperand SOp2 => sop2;
            public DstOperand DOp  => dop;

            private InstFlags flags;
            public InstFlags Flags => flags;

            public MIN(ARBConverter arb, string[] op, Mode mode, Modifier m, InstFlags flags)
            {
                this.flags = flags;
                sop1 = new SrcOperand(arb, mode, m, op[1]);
                sop2 = new SrcOperand(arb, mode, m, op[2]);
                dop  = new DstOperand(arb, mode, m, op[0]);
            }

            public override string ToString() =>
                $"{InstToString(Name, Flags)} {dop}, {sop1}, {sop2}";
        }

        public struct MOV : IInstruction
        {
            private SrcOperand sop;
            private DstOperand dop;

            public string Name => "MOV";
            public SrcOperand SOp => sop;
            public DstOperand DOp => dop;

            private InstFlags flags;
            public InstFlags Flags => flags;

            public MOV(ARBConverter arb, string[] op, Mode mode, Modifier m, InstFlags flags)
            {
                this.flags = flags;
                sop = new SrcOperand(arb, mode, m, op[1]);
                dop  = new DstOperand(arb, mode, m, op[0]);
                InstToString(Name, Flags);
            }

            public override string ToString() =>
                $"{InstToString(Name, Flags)} {dop}, {sop}";
        }

        public struct MUL : IInstruction
        {
            private SrcOperand sop1;
            private SrcOperand sop2;
            private DstOperand dop;

            public string Name => "MUL";
            public SrcOperand SOp1 => sop1;
            public SrcOperand SOp2 => sop2;
            public DstOperand DOp  => dop;

            private InstFlags flags;
            public InstFlags Flags => flags;

            public MUL(ARBConverter arb, string[] op, Mode mode, Modifier m, InstFlags flags)
            {
                this.flags = flags;
                sop1 = new SrcOperand(arb, mode, m, op[1]);
                sop2 = new SrcOperand(arb, mode, m, op[2]);
                dop  = new DstOperand(arb, mode, m, op[0]);
            }

            public override string ToString() =>
                $"{InstToString(Name, Flags)} {dop}, {sop1}, {sop2}";
        }

        public struct NRM : IInstruction
        {
            private SrcOperand sop;
            private DstOperand dop;

            public string Name => "NRM";
            public SrcOperand SOp => sop;
            public DstOperand DOp => dop;

            private InstFlags flags;
            public InstFlags Flags => flags;

            public NRM(ARBConverter arb, string[] op, Mode mode, Modifier m, InstFlags flags)
            {
                this.flags = flags;
                sop = new SrcOperand(arb, mode, m, op[1]);
                dop = new DstOperand(arb, mode, m, op[0]);
            }

            public override string ToString() =>
                $"{InstToString(Name, Flags)} {dop}, {sop}";
        }

        public struct POW : IInstruction
        {
            private SrcOperand sop1;
            private SrcOperand sop2;
            private DstOperand dop;

            public string Name => "POW";
            public SrcOperand SOp1 => sop1;
            public SrcOperand SOp2 => sop2;
            public DstOperand DOp  => dop;

            private InstFlags flags;
            public InstFlags Flags => flags;

            public POW(ARBConverter arb, string[] op, Mode mode, Modifier m, InstFlags flags)
            {
                this.flags = flags;
                sop1 = new SrcOperand(arb, mode, m, op[1]);
                sop2 = new SrcOperand(arb, mode, m, op[2]);
                dop  = new DstOperand(arb, mode, m, op[0]);
            }

            public override string ToString() =>
                $"{InstToString(Name, Flags)} {dop}, {sop1}, {sop2}";
        }

        public struct RCC : IInstruction
        {
            private SrcOperand sop;
            private DstOperand dop;

            public string Name => "RCC";
            public SrcOperand SOp => sop;
            public DstOperand DOp => dop;

            private InstFlags flags;
            public InstFlags Flags => flags;

            public RCC(ARBConverter arb, string[] op, Mode mode, Modifier m, InstFlags flags)
            {
                this.flags = flags;
                sop = new SrcOperand(arb, mode, m, op[1]);
                dop = new DstOperand(arb, mode, m, op[0]);
            }

            public override string ToString() =>
                $"{InstToString(Name, Flags)} {dop}, {sop}";
        }

        public struct RCP : IInstruction
        {
            private SrcOperand sop;
            private DstOperand dop;

            public string Name => "RCP";
            public SrcOperand SOp => sop;
            public DstOperand DOp => dop;

            private InstFlags flags;
            public InstFlags Flags => flags;

            public RCP(ARBConverter arb, string[] op, Mode mode, Modifier m, InstFlags flags)
            {
                this.flags = flags;
                sop = new SrcOperand(arb, mode, m, op[1]);
                dop = new DstOperand(arb, mode, m, op[0]);
            }

            public override string ToString() =>
                $"{InstToString(Name, Flags)} {dop}, {sop}";
        }

        public struct RFL : IInstruction
        {
            private SrcOperand sop1;
            private SrcOperand sop2;
            private DstOperand dop;

            public string Name => "RFL";
            public SrcOperand SOp1 => sop1;
            public SrcOperand SOp2 => sop2;
            public DstOperand DOp  => dop;

            private InstFlags flags;
            public InstFlags Flags => flags;

            public RFL(ARBConverter arb, string[] op, Mode mode, Modifier m, InstFlags flags)
            {
                this.flags = flags;
                sop1 = new SrcOperand(arb, mode, m, op[1]);
                sop2 = new SrcOperand(arb, mode, m, op[2]);
                dop  = new DstOperand(arb, mode, m, op[0]);
            }

            public override string ToString() =>
                $"{InstToString(Name, Flags)} {dop}, {sop1}, {sop2}";
        }

        public struct RSQ : IInstruction
        {
            private SrcOperand sop;
            private DstOperand dop;

            public string Name => "RSQ";
            public SrcOperand SOp => sop;
            public DstOperand DOp => dop;

            private InstFlags flags;
            public InstFlags Flags => flags;

            public RSQ(ARBConverter arb, string[] op, Mode mode, Modifier m, InstFlags flags)
            {
                this.flags = flags;
                sop = new SrcOperand(arb, mode, m, op[1]);
                dop = new DstOperand(arb, mode, m, op[0]);
            }

            public override string ToString() =>
                $"{InstToString(Name, Flags)} {dop}, {sop}";
        }

        public struct SCS : IInstruction
        {
            private SrcOperand sop;
            private DstOperand dop;

            public string Name => "SCS";
            public SrcOperand SOp => sop;
            public DstOperand DOp => dop;

            private InstFlags flags;
            public InstFlags Flags => flags;

            public SCS(ARBConverter arb, string[] op, Mode mode, Modifier m, InstFlags flags)
            {
                this.flags = flags;
                sop = new SrcOperand(arb, mode, m, op[1]);
                dop = new DstOperand(arb, mode, m, op[0]);
            }

            public override string ToString() =>
                $"{InstToString(Name, Flags)} {dop}, {sop}";
        }

        public struct SEQ : IInstruction
        {
            private SrcOperand sop1;
            private SrcOperand sop2;
            private DstOperand dop;

            public string Name => "SEQ";
            public SrcOperand SOp1 => sop1;
            public SrcOperand SOp2 => sop2;
            public DstOperand DOp  => dop;

            private InstFlags flags;
            public InstFlags Flags => flags;

            public SEQ(ARBConverter arb, string[] op, Mode mode, Modifier m, InstFlags flags)
            {
                this.flags = flags;
                sop1 = new SrcOperand(arb, mode, m, op[1]);
                sop2 = new SrcOperand(arb, mode, m, op[2]);
                dop  = new DstOperand(arb, mode, m, op[0]);
            }

            public override string ToString() =>
                $"{InstToString(Name, Flags)} {dop}, {sop1}, {sop2}";
        }

        public struct SFL : IInstruction
        {
            private SrcOperand sop1;
            private SrcOperand sop2;
            private DstOperand dop;

            public string Name => "SFL";
            public SrcOperand SOp1 => sop1;
            public SrcOperand SOp2 => sop2;
            public DstOperand DOp  => dop;

            private InstFlags flags;
            public InstFlags Flags => flags;

            public SFL(ARBConverter arb, string[] op, Mode mode, Modifier m, InstFlags flags)
            {
                this.flags = flags;
                sop1 = new SrcOperand(arb, mode, m, op[1]);
                sop2 = new SrcOperand(arb, mode, m, op[2]);
                dop  = new DstOperand(arb, mode, m, op[0]);
            }

            public override string ToString() =>
                $"{InstToString(Name, Flags)} {dop}, {sop1}, {sop2}";
        }

        public struct SGE : IInstruction
        {
            private SrcOperand sop1;
            private SrcOperand sop2;
            private DstOperand dop;

            public string Name => "SGE";
            public SrcOperand SOp1 => sop1;
            public SrcOperand SOp2 => sop2;
            public DstOperand DOp  => dop;

            private InstFlags flags;
            public InstFlags Flags => flags;

            public SGE(ARBConverter arb, string[] op, Mode mode, Modifier m, InstFlags flags)
            {
                this.flags = flags;
                sop1 = new SrcOperand(arb, mode, m, op[1]);
                sop2 = new SrcOperand(arb, mode, m, op[2]);
                dop  = new DstOperand(arb, mode, m, op[0]);
            }

            public override string ToString() =>
                $"{InstToString(Name, Flags)} {dop}, {sop1}, {sop2}";
        }

        public struct SGT : IInstruction
        {
            private SrcOperand sop1;
            private SrcOperand sop2;
            private DstOperand dop;

            public string Name => "SGT";
            public SrcOperand SOp1 => sop1;
            public SrcOperand SOp2 => sop2;
            public DstOperand DOp  => dop;

            private InstFlags flags;
            public InstFlags Flags => flags;

            public SGT(ARBConverter arb, string[] op, Mode mode, Modifier m, InstFlags flags)
            {
                this.flags = flags;
                sop1 = new SrcOperand(arb, mode, m, op[1]);
                sop2 = new SrcOperand(arb, mode, m, op[2]);
                dop  = new DstOperand(arb, mode, m, op[0]);
            }

            public override string ToString() =>
                $"{InstToString(Name, Flags)} {dop}, {sop1}, {sop2}";
        }

        public struct SIN : IInstruction
        {
            private SrcOperand sop;
            private DstOperand dop;

            public string Name => "SIN";
            public SrcOperand SOp => sop;
            public DstOperand DOp => dop;

            private InstFlags flags;
            public InstFlags Flags => flags;

            public SIN(ARBConverter arb, string[] op, Mode mode, Modifier m, InstFlags flags)
            {
                this.flags = flags;
                sop = new SrcOperand(arb, mode, m, op[1]);
                dop = new DstOperand(arb, mode, m, op[0]);
            }

            public override string ToString() =>
                $"{InstToString(Name, Flags)} {dop}, {sop}";
        }

        public struct SLE : IInstruction
        {
            private SrcOperand sop1;
            private SrcOperand sop2;
            private DstOperand dop;

            public string Name => "SLE";
            public SrcOperand SOp1 => sop1;
            public SrcOperand SOp2 => sop2;
            public DstOperand DOp  => dop;

            private InstFlags flags;
            public InstFlags Flags => flags;

            public SLE(ARBConverter arb, string[] op, Mode mode, Modifier m, InstFlags flags)
            {
                this.flags = flags;
                sop1 = new SrcOperand(arb, mode, m, op[1]);
                sop2 = new SrcOperand(arb, mode, m, op[2]);
                dop  = new DstOperand(arb, mode, m, op[0]);
            }

            public override string ToString() =>
                $"{InstToString(Name, Flags)} {dop}, {sop1}, {sop2}";
        }

        public struct SLT : IInstruction
        {
            private SrcOperand sop1;
            private SrcOperand sop2;
            private DstOperand dop;

            public string Name => "SLT";
            public SrcOperand SOp1 => sop1;
            public SrcOperand SOp2 => sop2;
            public DstOperand DOp  => dop;

            private InstFlags flags;
            public InstFlags Flags => flags;

            public SLT(ARBConverter arb, string[] op, Mode mode, Modifier m, InstFlags flags)
            {
                this.flags = flags;
                sop1 = new SrcOperand(arb, mode, m, op[1]);
                sop2 = new SrcOperand(arb, mode, m, op[2]);
                dop  = new DstOperand(arb, mode, m, op[0]);
            }

            public override string ToString() =>
                $"{InstToString(Name, Flags)} {dop}, {sop1}, {sop2}";
        }

        public struct SNE : IInstruction
        {
            private SrcOperand sop1;
            private SrcOperand sop2;
            private DstOperand dop;

            public string Name => "SNE";
            public SrcOperand SOp1 => sop1;
            public SrcOperand SOp2 => sop2;
            public DstOperand DOp  => dop;

            private InstFlags flags;
            public InstFlags Flags => flags;

            public SNE(ARBConverter arb, string[] op, Mode mode, Modifier m, InstFlags flags)
            {
                this.flags = flags;
                sop1 = new SrcOperand(arb, mode, m, op[1]);
                sop2 = new SrcOperand(arb, mode, m, op[2]);
                dop  = new DstOperand(arb, mode, m, op[0]);
            }

            public override string ToString() =>
                $"{InstToString(Name, Flags)} {dop}, {sop1}, {sop2}";
        }

        public struct SSG : IInstruction
        {
            private SrcOperand sop;
            private DstOperand dop;

            public string Name => "SSG";
            public SrcOperand SOp => sop;
            public DstOperand DOp => dop;

            private InstFlags flags;
            public InstFlags Flags => flags;

            public SSG(ARBConverter arb, string[] op, Mode mode, Modifier m, InstFlags flags)
            {
                this.flags = flags;
                sop = new SrcOperand(arb, mode, m, op[1]);
                dop = new DstOperand(arb, mode, m, op[0]);
            }

            public override string ToString() =>
                $"{InstToString(Name, Flags)} {dop}, {sop}";
        }

        public struct STR : IInstruction
        {
            private SrcOperand sop1;
            private SrcOperand sop2;
            private DstOperand dop;

            public string Name => "STR";
            public SrcOperand SOp1 => sop1;
            public SrcOperand SOp2 => sop2;
            public DstOperand DOp  => dop;

            private InstFlags flags;
            public InstFlags Flags => flags;

            public STR(ARBConverter arb, string[] op, Mode mode, Modifier m, InstFlags flags)
            {
                this.flags = flags;
                sop1 = new SrcOperand(arb, mode, m, op[1]);
                sop2 = new SrcOperand(arb, mode, m, op[2]);
                dop  = new DstOperand(arb, mode, m, op[0]);
            }

            public override string ToString() =>
                $"{InstToString(Name, Flags)} {dop}, {sop1}, {sop2}";
        }

        public struct SUB : IInstruction
        {
            private SrcOperand sop1;
            private SrcOperand sop2;
            private DstOperand dop;

            public string Name => "SUB";
            public SrcOperand SOp1 => sop1;
            public SrcOperand SOp2 => sop2;
            public DstOperand DOp  => dop;

            private InstFlags flags;
            public InstFlags Flags => flags;

            public SUB(ARBConverter arb, string[] op, Mode mode, Modifier m, InstFlags flags)
            {
                this.flags = flags;
                sop1 = new SrcOperand(arb, mode, m, op[1]);
                sop2 = new SrcOperand(arb, mode, m, op[2]);
                dop  = new DstOperand(arb, mode, m, op[0]);
            }

            public override string ToString() =>
                $"{InstToString(Name, Flags)} {dop}, {sop1}, {sop2}";
        }

        public struct SWZ : IInstruction
        {
            private SrcOperand sop;
            private string xSwizzle;
            private string ySwizzle;
            private string zSwizzle;
            private string wSwizzle;
            private DstOperand dop;

            public string Name => "SWZ";
            public SrcOperand SOp => sop;
            public string XSwizzle => xSwizzle;
            public string YSwizzle => ySwizzle;
            public string ZSwizzle => zSwizzle;
            public string WSwizzle => wSwizzle;
            public DstOperand DOp => dop;

            private InstFlags flags;
            public InstFlags Flags => flags;

            private bool negate;
            public bool Negate => negate;

            public SWZ(ARBConverter arb, string[] op, Mode mode, Modifier m, InstFlags flags)
            {
                this.flags = flags;
                sop = new SrcOperand(arb, mode, m, op[1]);
                xSwizzle = checkSwizzle(op[2]);
                ySwizzle = checkSwizzle(op[3]);
                zSwizzle = checkSwizzle(op[4]);
                wSwizzle = checkSwizzle(op[5]);
                dop = new DstOperand(arb, mode, m, op[0]);
                negate = op[1].StartsWith("-");

                static string checkSwizzle(string swz)
                {
                    if (swz == null || swz.Length != 1)
                        throw new Exception($"0x0053 Invalid swizzle \"{swz}\"");
                    return swz[0] switch
                    {
                        'x' => "x",
                        'y' => "y",
                        'z' => "z",
                        'w' => "w",
                        'r' => "x",
                        'g' => "y",
                        'b' => "z",
                        'a' => "w",
                        '1' => "1",
                        '0' => "0",
                        _ => throw new Exception($"0x0054 Invalid swizzle \"{swz}\""),
                    };
                }
            }

            public override string ToString() =>
                $"{InstToString(Name, Flags)} {dop}, {sop}, {xSwizzle},{ySwizzle},{zSwizzle},{wSwizzle}";
        }

        public struct TEX : IInstruction
        {
            private SrcOperand uv;
            private        int index;
            private    TexMode mode;
            private DstOperand dop;
            private     string offset;

            public string Name => "TEX";
            public SrcOperand UV     => uv;
            public        int Index  => index;
            public    TexMode Mode   => mode;
            public DstOperand DOp    => dop;
            public     string Offset => offset;

            private InstFlags flags;
            public InstFlags Flags => flags;

            public TEX(ARBConverter arb, string[] op, Mode mode, Modifier m, InstFlags flags)
            {
                this.flags = flags;
                uv = new SrcOperand(arb, mode, m, op[1]);
                dop = new DstOperand(arb, mode, m, op[0]);
                if (!op[2].StartsWith("texture[") || !op[2].EndsWith("]")
                    || !int.TryParse(op[2].Replace("texture[", "").Replace("]", ""), out index))
                    throw new Exception($"0x003E Invalid TEX param \"{op[2]}\"");

                if (!Enum.TryParse("_" + op[3], out this.mode))
                    throw new Exception($"0x004B Invalid TEX param \"{op[3]}\"");

                offset = op.Length > 4 ? op[4] : null;
            }

            public override string ToString() =>
                $"{InstToString(Name, Flags)} {dop}, {uv}, texture[{index}], {mode.ToString().Replace("_", "")}";
        }

        public struct TRUNC : IInstruction
        {
            private SrcOperand sop;
            private DstOperand dop;

            public string Name => "TRUNC";
            public SrcOperand SOp => sop;
            public DstOperand DOp => dop;

            private InstFlags flags;
            public InstFlags Flags => flags;

            public TRUNC(ARBConverter arb, string[] op, Mode mode, Modifier m, InstFlags flags)
            {
                this.flags = flags;
                sop = new SrcOperand(arb, mode, m, op[1]);
                dop = new DstOperand(arb, mode, m, op[0]);
            }

            public override string ToString() =>
                $"{InstToString(Name, Flags)} {dop}, {sop}";
        }

        public struct TXB : IInstruction
        {
            private SrcOperand uv;
            private        int index;
            private    TexMode mode;
            private DstOperand dop;
            private     string offset;

            public string Name => "TXB";
            public SrcOperand UV     => uv;
            public        int Index  => index;
            public    TexMode Mode   => mode;
            public DstOperand DOp    => dop;
            public     string Offset => offset;

            private InstFlags flags;
            public InstFlags Flags => flags;

            public TXB(ARBConverter arb, string[] op, Mode mode, Modifier m, InstFlags flags)
            {
                this.flags = flags;
                uv = new SrcOperand(arb, mode, m, op[1]);
                dop = new DstOperand(arb, mode, m, op[0]);
                if (!op[2].StartsWith("texture[") || !op[2].EndsWith("]")
                    || !int.TryParse(op[2].Replace("texture[", "").Replace("]", ""), out index))
                    throw new Exception($"0x004C Invalid TXB param \"{op[2]}\"");

                if (!Enum.TryParse("_" + op[3], out this.mode))
                    throw new Exception($"0x004D Invalid TXB param \"{op[3]}\"");

                offset = op.Length > 4 ? op[4] : null;
            }

            public override string ToString() =>
                $"{InstToString(Name, Flags)} {dop}, {uv}, texture[{index}], {mode.ToString().Replace("_", "")}";
        }

        public struct TXL : IInstruction
        {
            private SrcOperand uv;
            private        int index;
            private    TexMode mode;
            private DstOperand dop;
            private     string offset;

            public string Name => "TXL";
            public SrcOperand UV     => uv;
            public        int Index  => index;
            public    TexMode Mode   => mode;
            public DstOperand DOp    => dop;
            public     string Offset => offset;

            private InstFlags flags;
            public InstFlags Flags => flags;

            public TXL(ARBConverter arb, string[] op, Mode mode, Modifier m, InstFlags flags)
            {
                this.flags = flags;
                uv = new SrcOperand(arb, mode, m, op[1]);
                dop = new DstOperand(arb, mode, m, op[0]);
                if (!op[2].StartsWith("texture[") || !op[2].EndsWith("]")
                    || !int.TryParse(op[2].Replace("texture[", "").Replace("]", ""), out index))
                    throw new Exception($"0x004F Invalid TXL param \"{op[2]}\"");

                if (!Enum.TryParse("_" + op[3], out this.mode))
                    throw new Exception($"0x0050 Invalid TXL param \"{op[3]}\"");

                offset = op.Length > 4 ? op[4] : null;
            }

            public override string ToString() =>
                $"{InstToString(Name, Flags)} {dop}, {uv}, texture[{index}], {mode.ToString().Replace("_", "")}";
        }

        public struct TXP : IInstruction
        {
            private SrcOperand uv;
            private        int index;
            private    TexMode mode;
            private DstOperand dop;
            private     string offset;

            public string Name => "TXP";
            public SrcOperand UV     => uv;
            public        int Index  => index;
            public    TexMode Mode   => mode;
            public DstOperand DOp    => dop;
            public     string Offset => offset;

            private InstFlags flags;
            public InstFlags Flags => flags;

            public TXP(ARBConverter arb, string[] op, Mode mode, Modifier m, InstFlags flags)
            {
                this.flags = flags;
                uv = new SrcOperand(arb, mode, m, op[1]);
                dop = new DstOperand(arb, mode, m, op[0]);
                if (!op[2].StartsWith("texture[") || !op[2].EndsWith("]")
                    || !int.TryParse(op[2].Replace("texture[", "").Replace("]", ""), out index))
                    throw new Exception($"0x0051 Invalid TXP param \"{op[2]}\"");

                if (!Enum.TryParse("_" + op[3], out this.mode))
                    throw new Exception($"0x0052 Invalid TXP param \"{op[3]}\"");

                offset = op.Length > 4 ? op[4] : null;
            }

            public override string ToString() =>
                $"{InstToString(Name, Flags)} {dop}, {uv}, texture[{index}], {mode.ToString().Replace("_", "")}";
        }

        public struct X2D : IInstruction
        {
            private SrcOperand sop1;
            private SrcOperand sop2;
            private SrcOperand sop3;
            private DstOperand dop;

            public string Name => "X2D";
            public SrcOperand SOp1 => sop1;
            public SrcOperand SOp2 => sop2;
            public SrcOperand SOp3 => sop3;
            public DstOperand DOp  => dop;

            private InstFlags flags;
            public InstFlags Flags => flags;

            public X2D(ARBConverter arb, string[] op, Mode mode, Modifier m, InstFlags flags)
            {
                this.flags = flags;
                sop1 = new SrcOperand(arb, mode, m, op[1]);
                sop2 = new SrcOperand(arb, mode, m, op[2]);
                sop3 = new SrcOperand(arb, mode, m, op[3]);
                dop  = new DstOperand(arb, mode, m, op[0]);
            }

            public override string ToString() =>
                $"{InstToString(Name, Flags)} {dop}, {sop1}, {sop2}, {sop3}";
        }

        public struct XPD : IInstruction
        {
            private SrcOperand sop1;
            private SrcOperand sop2;
            private DstOperand dop;

            public string Name => "XPD";
            public SrcOperand SOp1 => sop1;
            public SrcOperand SOp2 => sop2;
            public DstOperand DOp  => dop;

            private InstFlags flags;
            public InstFlags Flags => flags;

            public XPD(ARBConverter arb, string[] op, Mode mode, Modifier m, InstFlags flags)
            {
                this.flags = flags;
                sop1 = new SrcOperand(arb, mode, m, op[1]);
                sop2 = new SrcOperand(arb, mode, m, op[2]);
                dop  = new DstOperand(arb, mode, m, op[0]);
            }

            public override string ToString() =>
                $"{InstToString(Name, Flags)} {dop}, {sop1}, {sop2}";
        }
        #endregion

        #region CCInst
        public struct BRA : IInstruction
        {
            public string Name => "BRA";
            public SrcOperand SOp => new SrcOperand(Var.None);
            public DstOperand DOp => new DstOperand(Var.None);

            public InstFlags Flags => InstFlags.N;

            private CC cc;
            public CC CC => cc;

            private string label;
            public string Label => label;

            public BRA(string[] op, List<string> label)
            {
                if (op == null || op.Length != 1)
                    throw new Exception("0x0097 Invalid BRA op");

                string[] str = op[0].Split('(');
                if (str.Length != 2)
                    throw new Exception("0x0098 Invalid BRA op");

                this.label = str[0];
                str[1] = "(" + str[1];
                label.Add(str[0]);
                cc = new CC(ref str[1]);
            }

            public override string ToString() =>
                cc.Op == CCOp.None ? Name : $"{Name}{cc.ToString(false)}";
        }

        public struct CAL : IInstruction
        {
            public string Name => "CAL";
            public SrcOperand SOp => new SrcOperand(Var.None);
            public DstOperand DOp => new DstOperand(Var.None);

            public InstFlags Flags => InstFlags.N;

            private CC cc;
            public CC CC => cc;

            private string label;
            public string Label => label;

            public CAL(string[] op, List<string> label)
            {
                if (op == null || op.Length != 1)
                    throw new Exception("0x009C Invalid CAL op");

                string[] str = op[0].Split('(');
                if (str.Length != 2)
                    throw new Exception("0x009D Invalid CAL op");

                this.label = str[0];
                str[1] = "(" + str[1];
                label.Add(str[0]);
                cc = new CC(ref str[1]);
            }

            public override string ToString() =>
                cc.Op == CCOp.None ? Name : $"{Name}{cc.ToString(false)}";
        }

        public struct ELSE : IInstruction
        {
            public string Name => "ELSE";
            public SrcOperand SOp => new SrcOperand(Var.None);
            public DstOperand DOp => new DstOperand(Var.None);

            public InstFlags Flags => InstFlags.N;

            public override string ToString() =>
                Name;
        }

        public struct ENDIF : IInstruction
        {
            public string Name => "ENDIF";
            public SrcOperand SOp => new SrcOperand(Var.None);
            public DstOperand DOp => new DstOperand(Var.None);

            public InstFlags Flags => InstFlags.N;

            public override string ToString() =>
                Name;
        }

        public struct ENDLOOP : IInstruction
        {
            public string Name => "ENDLOOP";
            public SrcOperand SOp => new SrcOperand(Var.None);
            public DstOperand DOp => new DstOperand(Var.None);

            public InstFlags Flags => InstFlags.N;

            public override string ToString() =>
                Name;
        }

        public struct ENDREP : IInstruction
        {
            public string Name => "ENDREP";
            public SrcOperand SOp => new SrcOperand(Var.None);
            public DstOperand DOp => new DstOperand(Var.None);

            public InstFlags Flags => InstFlags.N;

            public override string ToString() =>
                Name;
        }

        public struct IF : IInstruction
        {
            public string Name => "IF";
            public SrcOperand SOp => new SrcOperand(Var.None);
            public DstOperand DOp => new DstOperand(Var.None);

            public InstFlags Flags => InstFlags.N;

            private CC cc;
            public CC CC => cc;

            public IF(string[] op)
            {
                if (op == null || op.Length != 1)
                    throw new Exception("0x0096 Invalid IF op");

                string str = $"({op[0]})";
                cc = new CC(ref str);
            }

            public override string ToString() =>
                cc.Op == CCOp.None ? Name : $"{Name}{cc.ToString(false)}";
        }

        public struct KIL : IInstruction
        {
            private SrcOperand sop;

            public string Name => "KIL";
            public SrcOperand SOp => sop;
            public DstOperand DOp => new DstOperand(Var.None);

            public InstFlags Flags => InstFlags.N;

            private CC cc;
            public CC CC => cc;

            public KIL(ARBConverter arb, string[] op, Mode mode, Modifier m, InstFlags flags)
            {
                if (op == null || op.Length != 1)
                    throw new Exception("0x0061 Invalid KIL op");

                string str = op[0];
                cc = new CC(ref str, true);
                if (cc.Op == CCOp.None)
                    sop = new SrcOperand(arb, mode, m, op[0]);
                else
                    sop = new SrcOperand(Var.None);
            }

            public override string ToString() =>
                cc.Op == CCOp.None ? $"{Name} {sop}" : $"{Name}{cc}";
        }

        public struct Label : IInstruction
        {
            private string name;

            public string Name => name;
            public DstOperand DOp => new DstOperand(Var.None);

            public InstFlags Flags => InstFlags.N;

            public Label(string name)
            {
                this.name = name;
            }

            public override string ToString() =>
                name;
        }

        public struct LOOP : IInstruction
        {
            private SrcOperand sop;

            public string Name => "LOOP";
            public SrcOperand SOp => sop;
            public DstOperand DOp => new DstOperand(Var.None);

            public InstFlags Flags => InstFlags.N;

            public LOOP(ARBConverter arb, string[] op, Mode mode, Modifier m, InstFlags flags)
            {
                sop = new SrcOperand(arb, mode, m, op[0]);
            }

            public override string ToString() =>
                $"{Name} {sop}";
        }

        public struct REP : IInstruction
        {
            private SrcOperand sop;

            public string Name => "REP";
            public SrcOperand SOp => sop;
            public DstOperand DOp => new DstOperand(Var.None);

            public InstFlags Flags => InstFlags.N;

            public REP(ARBConverter arb, string[] op, Mode mode, Modifier m, InstFlags flags)
            {
                sop = new SrcOperand(arb, mode, m, op[0]);
            }

            public override string ToString() =>
                $"{Name} {sop}";
        }

        public struct RET : IInstruction
        {
            public string Name => "RET";
            public SrcOperand SOp => new SrcOperand(Var.None);
            public DstOperand DOp => new DstOperand(Var.None);

            public InstFlags Flags => InstFlags.N;

            private CC cc;
            public CC CC => cc;

            public RET(ARBConverter arb, string[] op, Mode mode, Modifier m, InstFlags flags)
            {
                if (op == null || op.Length != 1)
                { cc = default; return; }

                string str = op[0];
                cc = new CC(ref str, true);
            }

            public override string ToString() =>
                cc.Op == CCOp.None ? Name : $"{Name}{cc}";
        }
        #endregion

        #region DummyInst
        public struct Dummy0 : IInstruction
        {
            private string name;

            public string Name => name;
            public DstOperand DOp => new DstOperand(Var.None);

            private InstFlags flags;
            public InstFlags Flags => flags;

            public Dummy0(ARBConverter arb, string[] op, Mode mode, Modifier m, InstFlags flags, string name)
            {
                this.flags = flags;
                this.name = name;
            }

            public override string ToString() =>
                $"{name}";
        }

        public struct Dummy1 : IInstruction
        {
            private string name;
            private SrcOperand sop;

            public string Name => name;
            public SrcOperand SOp => sop;
            public DstOperand DOp => new DstOperand(Var.None);

            private InstFlags flags;
            public InstFlags Flags => flags;

            public Dummy1(ARBConverter arb, string[] op, Mode mode, Modifier m, InstFlags flags, string name)
            {
                this.flags = flags;
                this.name = name;
                sop = new SrcOperand(arb, mode, m, op[0]);
            }

            public override string ToString() =>
                $"{name} {sop}";
        }

        public struct Dummy2 : IInstruction
        {
            private string name;
            private SrcOperand sop;
            private DstOperand dop;

            public string Name => name;
            public SrcOperand SOp => sop;
            public DstOperand DOp => dop;

            private InstFlags flags;
            public InstFlags Flags => flags;

            public Dummy2(ARBConverter arb, string[] op, Mode mode, Modifier m, InstFlags flags, string name)
            {
                this.flags = flags;
                this.name = name;
                sop = new SrcOperand(arb, mode, m, op[1]);
                dop = new DstOperand(arb, mode, m, op[0]);
            }

            public override string ToString() =>
                $"{InstToString(Name, Flags)} {dop}, {sop}";
        }

        public struct Dummy3 : IInstruction
        {
            private string name;
            private SrcOperand sop1;
            private SrcOperand sop2;
            private DstOperand dop;

            public string Name => name;
            public SrcOperand SOp1 => sop1;
            public SrcOperand SOp2 => sop2;
            public DstOperand DOp  => dop;

            private InstFlags flags;
            public InstFlags Flags => flags;

            public Dummy3(ARBConverter arb, string[] op, Mode mode, Modifier m, InstFlags flags, string name)
            {
                this.flags = flags;
                this.name = name;
                sop1 = new SrcOperand(arb, mode, m, op[1]);
                sop2 = new SrcOperand(arb, mode, m, op[2]);
                dop  = new DstOperand(arb, mode, m, op[0]);
            }

            public override string ToString() =>
                $"{InstToString(Name, Flags)} {dop}, {sop1}, {sop2}";
        }

        public struct Dummy4 : IInstruction
        {
            private string name;
            private SrcOperand sop1;
            private SrcOperand sop2;
            private SrcOperand sop3;
            private DstOperand dop;

            public string Name => name;
            public SrcOperand SOp1 => sop1;
            public SrcOperand SOp2 => sop2;
            public SrcOperand SOp3 => sop3;
            public DstOperand DOp  => dop;

            private InstFlags flags;
            public InstFlags Flags => flags;

            public Dummy4(ARBConverter arb, string[] op, Mode mode, Modifier m, InstFlags flags, string name)
            {
                this.flags = flags;
                this.name = name;
                sop1 = new SrcOperand(arb, mode, m, op[1]);
                sop2 = new SrcOperand(arb, mode, m, op[2]);
                sop3 = new SrcOperand(arb, mode, m, op[3]);
                dop  = new DstOperand(arb, mode, m, op[0]);
            }

            public override string ToString() =>
                $"{InstToString(Name, Flags)} {dop}, {sop1}, {sop2}, {sop3}";
        }

        public struct Dummy5 : IInstruction
        {
            private string name;
            private SrcOperand sop1;
            private SrcOperand sop2;
            private SrcOperand sop3;
            private SrcOperand sop4;
            private DstOperand dop;

            public string Name => name;
            public SrcOperand SOp1 => sop1;
            public SrcOperand SOp2 => sop2;
            public SrcOperand SOp3 => sop3;
            public SrcOperand SOp4 => sop4;
            public DstOperand DOp  => dop;

            private InstFlags flags;
            public InstFlags Flags => flags;

            public Dummy5(ARBConverter arb, string[] op, Mode mode, Modifier m, InstFlags flags, string name)
            {
                this.flags = flags;
                this.name = name;
                sop1 = new SrcOperand(arb, mode, m, op[1]);
                sop2 = new SrcOperand(arb, mode, m, op[2]);
                sop3 = new SrcOperand(arb, mode, m, op[3]);
                sop4 = new SrcOperand(arb, mode, m, op[4]);
                dop  = new DstOperand(arb, mode, m, op[0]);
            }

            public override string ToString() =>
                $"{InstToString(Name, Flags)} {dop}, {sop1}, {sop2}, {sop3}, {sop4}";
        }
        #endregion
        #endregion

        [Flags]
        public enum InstFlags
        {
            N   = 0b000000000000,
            R   = 0b000000000001,
            H   = 0b000000000010,
            X   = 0b000000000100,
            C   = 0b000000001000,
            S   = 0b000000010000,
            s   = 0b000000100000,
            F   = 0b000001000000,
            I   = 0b000010000000,
            U   = 0b000100000000,
            CC  = 0b001000000000,
            CC0 = 0b010000000000,
            CC1 = 0b100000000000,
        }

        public enum TexMode
        {
            None,
            _1D,
            _2D,
            _3D,
            _CUBE,
            _RECT,
            _SHADOW1D,
            _SHADOW2D,
            _SHADOWRECT,
        }

        #region Name
        public interface IName
        {
            Modifier Mod { get; }
            string Var { get; }
        }

        #region NameAttrib
        public interface INameAttrib : IName
        { }

        public struct NameAttrib : INameAttrib
        {
            private Modifier mod;
            private string var;

            public Modifier Mod => mod;
            public string Var => var;

            public NameAttrib(Modifier mod, string var)
            { this.mod = mod; this.var = var; }

            public override string ToString() =>
                var != null && var != "" ? $"(Var: {var})" : "(None)";
        }

        public struct NameAttribIndex : INameAttrib
        {
            public int ID;

            private Modifier mod;
            private string var;

            public Modifier Mod => mod;
            public string Var => var;

            public NameAttribIndex(Modifier mod, string var, int id)
            { this.mod = mod; this.var = var; ID = id; }

            public override string ToString() =>
                var != null && var != "" ? $"(Var: {var}; ID: {ID})" : "(None)";
        }

        public struct NameAttribRange : INameAttrib
        {
            public int IDStart;
            public int IDEnd;

            private Modifier mod;
            private string var;

            public Modifier Mod => mod;
            public string Var => var;

            public NameAttribRange(Modifier mod, string var, int idStart, int idEnd)
            { this.mod = mod; this.var = var; IDStart = idStart; IDEnd = idEnd; }

            public override string ToString() =>
                var != null && var != "" ? $"(Var: {var}" + (IDStart == IDEnd ? $"; Dst ID: {IDStart})"
                : $"; Dst ID Start: {IDStart}; Dst ID End: {IDEnd})") : "(None)";
        }
        #endregion

        #region NameOutput
        public interface INameOutput : IName
        { }

        public struct NameOutput : INameOutput
        {
            private Modifier mod;
            private string var;

            public Modifier Mod => mod;
            public string Var => var;

            public NameOutput(Modifier mod, string var)
            { this.mod = mod; this.var = var; }

            public override string ToString() =>
                var != null && var != "" ? $"(Var: {var})" : "(None)";
        }

        public struct NameOutputIndex : INameOutput
        {
            public int ID;

            private Modifier mod;
            private string var;

            public Modifier Mod => mod;
            public string Var => var;

            public NameOutputIndex(Modifier mod, string var, int id)
            { this.mod = mod; this.var = var; ID = id; }

            public override string ToString() =>
                var != null && var != "" ? $"(Var: {var}; ID: {ID})" : "(None)";
        }

        public struct NameOutputRange : INameOutput
        {
            public int IDStart;
            public int IDEnd;

            private Modifier mod;
            private string var;

            public Modifier Mod => mod;
            public string Var => var;

            public NameOutputRange(Modifier mod, string var, int idStart, int idEnd)
            { this.mod = mod; this.var = var; IDStart = idStart; IDEnd = idEnd; }

            public override string ToString() =>
                var != null && var != "" ? $"(Var: {var}" + (IDStart == IDEnd ? $"; Dst ID: {IDStart})"
                : $"; Dst ID Start: {IDStart}; Dst ID End: {IDEnd})") : "(None)";
        }
        #endregion

        #region NameParam
        public interface INameParam : IName
        { }

        public struct NameParam : INameParam
        {
            private Modifier mod;
            private string var;

            public Modifier Mod => mod;
            public string Var => var;

            public NameParam(Modifier mod, string var)
            { this.mod = mod; this.var = var; }

            public override string ToString() =>
                var != null && var != "" ? $"(Var: {var})" : "(None)";
        }

        public struct NameParamIndex : INameParam
        {
            public int ID;

            private Modifier mod;
            private string var;

            public Modifier Mod => mod;
            public string Var => var;

            public NameParamIndex(Modifier mod, string var, int id)
            { this.mod = mod; this.var = var; ID = id; }

            public override string ToString() =>
                var != null && var != "" ? $"(Var: {var}; ID: {ID})" : "(None)";
        }

        public struct NameParamRange : INameParam
        {
            public int IDStart;
            public int IDEnd;

            private Modifier mod;
            private string var;

            public Modifier Mod => mod;
            public string Var => var;

            public NameParamRange(Modifier mod, string var, int idStart, int idEnd)
            { this.mod = mod; this.var = var; IDStart = idStart; IDEnd = idEnd; }

            public override string ToString() =>
                var != null && var != "" ? $"(Var: {var}" + (IDStart == IDEnd ? $"; Dst ID: {IDStart})"
                : $"; Dst ID Start: {IDStart}; Dst ID End: {IDEnd})") : "(None)";
        }
        #endregion
        #endregion

        public struct Address
        {
            private string var;

            public string Var => var;

            public Address(string var)
            { this.var = var; }

            public override string ToString() =>
                var != null && var != "" ? $"(Var: {var})" : "(None)";
        }

        public struct Temp
        {
            private Modifier mod;
            private string var;

            public Modifier Mod => mod;
            public string Var => var;

            public Temp(Modifier mod, string var)
            { this.mod = mod; this.var = var; }

            public override string ToString() =>
                var != null && var != "" ? $"(Var: {var})" : "(None)";
        }

        #region Type
        public interface IType
        {
            Var Var { get; }
            bool Abs { get; }
            Sign Sign { get; }
            bool IsVal { get; }
        }

        public struct Type : IType
        {
            private Var var;

            public Var Var => var;

            private bool abs;
            private Sign sign;

            public bool Abs => abs;
            public Sign Sign => sign;

            public bool IsVal => false;

            public Type(Var var, bool abs, Sign sign)
            { this.var = var; this.abs = abs; this.sign = sign; }

            public override string ToString() =>
                var > 0 ? $"(Var: {var})" : "(None)";
        }

        public struct TypeArray : IType
        {
            public IType[] Array;

            public Var Var => Var.Array;

            public bool Abs => false;
            public Sign Sign => Sign.N;

            public bool IsVal => true;

            public TypeArray(IType[] array)
            { Array = array;  }

            public override string ToString() =>
                Array != null && Array.Length > 0 ? $"(Array Length: {Array.Length})" : "(None)";
        }

        public struct TypeIndex : IType
        {
            public int ID;

            private Var var;

            public Var Var => var;

            private bool abs;
            private Sign sign;

            public bool Abs => abs;
            public Sign Sign => sign;

            public bool IsVal => false;

            public TypeIndex(Var var, bool abs, Sign sign, int id)
            { this.var = var; this.abs = abs; this.sign = sign; ID = id; }

            public override string ToString() =>
                var > 0 ? $"(Var: {var}; ID: {ID})" : "(None)";
        }

        public struct TypeIndexIndex : IType
        {
            public int ID0;
            public int ID1;

            private Var var;

            public Var Var => var;

            private bool abs;
            private Sign sign;

            public bool Abs => abs;
            public Sign Sign => sign;

            public bool IsVal => false;

            public TypeIndexIndex(Var var, bool abs, Sign sign, int id0, int id1)
            { this.var = var; this.abs = abs; this.sign = sign; ID0 = id0; ID1 = id1; }

            public override string ToString() =>
                var > 0 ? $"(Var: {var}; ID 0: {ID0}; ID 1: {ID1})" : "(None)";
        }

        public struct TypeIndexName : IType
        {
            public string Name;
            public string Index;

            private Var var;

            public Var Var => var;

            private bool abs;
            private Sign sign;

            public bool Abs => abs;
            public Sign Sign => sign;

            public bool IsVal => false;

            public TypeIndexName(Var var, bool abs, Sign sign, string name, string index)
            { this.var = var; this.abs = abs; this.sign = sign; Name = name; Index = index; }

            public override string ToString() =>
                var > 0 ? $"(Var: {var}; Name: {Name}; Index: {Index})" : "(None)";
        }

        public struct TypeIndexRange : IType
        {
            public int ID;
            public int IDStart;
            public int IDEnd;

            private Var var;

            public Var Var => var;

            private bool abs;
            private Sign sign;

            public bool Abs => abs;
            public Sign Sign => sign;

            public bool IsVal => false;

            public TypeIndexRange(Var var, bool abs, Sign sign, int id, int idStart, int idEnd)
            { this.var = var; this.abs = abs; this.sign = sign; ID = id; IDStart = idStart; IDEnd = idEnd; }

            public override string ToString() =>
                var > 0 ? $"(Var: {var}; ID 0: {ID}" + (IDStart == IDEnd ? $"; ID 1: {IDStart})"
                : $"; ID 1 Start: {IDStart}; ID 1 End: {IDEnd})") : "(None)";
        }

        public struct TypeName : IType
        {
            private Var var;
            private string name;

            public Var Var => var;
            public string Name => name;

            private bool abs;
            private Sign sign;

            public bool Abs => abs;
            public Sign Sign => sign;

            public bool IsVal => false;

            public TypeName(string name, bool abs, Sign sign)
            { var = Var.Name; this.abs = abs; this.sign = sign; this.name = name; }

            public override string ToString() =>
                var > 0 ? $"(Var: {var}; Name: {name})" : "(None)";
        }

        public struct TypeRange : IType
        {
            public int IDStart;
            public int IDEnd;

            private Var var;

            public Var Var => var;

            private bool abs;
            private Sign sign;

            public bool Abs => abs;
            public Sign Sign => sign;

            public bool IsVal => false;

            public TypeRange(Var var, bool abs, Sign sign, int idStart, int idEnd)
            { this.var = var; this.abs = abs; this.sign = sign; IDStart = idStart; IDEnd = idEnd; }

            public override string ToString() =>
                var > 0 ? $"(Var: {var}" + (IDStart == IDEnd ? $"; Dst ID: {IDStart})"
                : $"; Dst ID Start: {IDStart}; Dst ID End: {IDEnd})") : "(None)";
        }

        public struct TypeVec<T> : IType
        {
            private T x;
            private T y;
            private T z;
            private T w;

            public T X => x;
            public T Y => y;
            public T Z => z;
            public T W => w;

            private Var var;
            public Var Var => var;

            private bool abs;
            private Sign sign;

            public bool Abs => abs;
            public Sign Sign => sign;

            public bool IsVal => true;

            public TypeVec(Var var, T x, T y, T z, T w, bool abs, Sign sign)
            { this.var = var; this.x = x; this.y = y; this.z = z; this.w = w; this.abs = abs; this.sign = sign; }

            public override string ToString() =>
                $"(X: {x}; Y: {y}; Z: {z}; W: {w})";
        }
        #endregion

        #region Vec4
        public interface IVec4
        { string ToString(); }

        public struct Vec4Int : IVec4
        {
            public float X;
            public float Y;
            public float Z;
            public float W;

            public Vec4Int(float x)
            { X = x; Y = Z = W = 0; }

            public Vec4Int(float x, float y)
            { X = x; Y = y; Z = W = 0; }

            public Vec4Int(float x, float y, float z)
            { X = x; Y = y; Z = z; W = 0; }

            public Vec4Int(float x, float y, float z, float w)
            { X = x; Y = y; Z = z; W = w; }

            public override string ToString() =>
                $"({X}, {Y}, {Z}, {W})";
        }

        public struct Vec4UInt : IVec4
        {
            public float X;
            public float Y;
            public float Z;
            public float W;

            public Vec4UInt(float x)
            { X = x; Y = Z = W = 0; }

            public Vec4UInt(float x, float y)
            { X = x; Y = y; Z = W = 0; }

            public Vec4UInt(float x, float y, float z)
            { X = x; Y = y; Z = z; W = 0; }

            public Vec4UInt(float x, float y, float z, float w)
            { X = x; Y = y; Z = z; W = w; }

            public override string ToString() =>
                $"({X}, {Y}, {Z}, {W})";
        }

        public struct Vec4Float : IVec4
        {
            public float X;
            public float Y;
            public float Z;
            public float W;

            public Vec4Float(float x)
            { X = x; Y = Z = W = 0.0f; }

            public Vec4Float(float x, float y)
            { X = x; Y = y; Z = W = 0.0f; }

            public Vec4Float(float x, float y, float z)
            { X = x; Y = y; Z = z; W = 0.0f; }

            public Vec4Float(float x, float y, float z, float w)
            { X = x; Y = y; Z = z; W = w; }

            public override string ToString() =>
                $"({X}, {Y}, {Z}, {W})";
        }
        #endregion

        public enum Modifier : int
        {
            FLOAT = 0, // F32
            SHORT = 1, // F16
             LONG = 2, // F32
              INT = 3, // I32
             UINT = 4, // U32
        }

        public enum Sign
        {
            N = 0, // none
            P = 1, // +
            M = 2, // -
        }

        public enum Var : int
        {
            Null = 0,
            None,

            // Vertex Shader Input
            VertexInputPosition,                    // vertex.position
            VertexInputWeight,                      // vertex.weight
            VertexInputWeightN,                     // vertex.weight[n]
            VertexInputNormal,                      // vertex.normal
            VertexInputColor,                       // vertex.color
            VertexInputColor1,                      // vertex.color.primary
            VertexInputColor2,                      // vertex.color.secondary
            VertexInputFogCoord,                    // vertex.fogcoord
            VertexInputTexCoord,                    // vertex.texcoord
            VertexInputTexCoordN,                   // vertex.texcoord[n]
            VertexInputMatrixIndex,                 // vertex.matrixindex
            VertexInputMatrixIndexN,                // vertex.matrixindex[n]
            VertexInputAttrib,                      // vertex.attrib
            VertexInputAttribN,                     // vertex.attrib[n]

            // Vertex Shader Output
            VertexOutputPosition,                   // result.position
            VertexOutputColor,                      // result.color
            VertexOutputColor1,                     // result.color.primary
            VertexOutputColor2,                     // result.color.secondary
            VertexOutputColorFront,                 // result.color.front
            VertexOutputColorFront1,                // result.color.front.primary
            VertexOutputColorFront2,                // result.color.front.secondary
            VertexOutputColorBack,                  // result.color.back
            VertexOutputColorBack1,                 // result.color.back.primary
            VertexOutputColorBack2,                 // result.color.back.secondary
            VertexOutputFogCoord,                   // result.fogcoord
            VertexOutputPointSize,                  // result.pointsize
            VertexOutputTexCoord,                   // result.texcoord
            VertexOutputTexCoordN,                  // result.texcoord[n]

            // Fragment Shader Input
            FragmentInputColor,                     // fragment.color
            FragmentInputColor1,                    // fragment.color.primary
            FragmentInputColor2,                    // fragment.color.secondary
            FragmentInputTexCoord,                  // fragment.texcoord
            FragmentInputTexCoordN,                 // fragment.texcoord[n]
            FragmentInputFogCoord,                  // fragment.fogcoord
            FragmentInputPosition,                  // fragment.position
            FragmentInputClipN,                     // fragment.clip[n]
            FragmentInputAttribN,                   // fragment.attrib[n]
            FragmentInputClipNO,                    // fragment.clip[n..o]
            FragmentInputFacing,                    // fragment.facing

            // Fragment Shader Output
            FragmentOutputColor,                    // result.color
            FragmentOutputColorN,                   // result.color[n]
            FragmentOutputDepth,                    // result.depth

            // Vertex Program Env/Local Bindings
            VertexProgramEnvN,                      // program.env[n]
            VertexProgramLocalN,                    // program.local[n]
            VertexProgramEnvNO,                     // program.env[n..o]
            VertexProgramLocalNO,                   // program.local[n..o]

            // Fragment Program Env/Local Bindings
            FragmentProgramEnvN,                    // program.env[n]
            FragmentProgramLocalN,                  // program.local[n]
            FragmentProgramEnvNO,                   // program.env[n..o]
            FragmentProgramLocalNO,                 // program.local[n..o]

            ProgramBufferN,                         // program.buffer[n]
            ProgramBufferNO,                        // program.buffer[n][o]
            ProgramBufferNOP,                       // program.buffer[n][o..p]

            // Material Bindings
            StateMaterialAmbient,                   // state.material.ambient
            StateMaterialDiffuse,                   // state.material.diffuse
            StateMaterialSpecular,                  // state.material.specular
            StateMaterialEmission,                  // state.material.emission
            StateMaterialShininess,                 // state.material.shininess
            StateMaterialFrontAmbient,              // state.material.front.ambient
            StateMaterialFrontDiffuse,              // state.material.front.diffuse
            StateMaterialFrontSpecular,             // state.material.front.specular
            StateMaterialFrontEmission,             // state.material.front.emission
            StateMaterialFrontShininess,            // state.material.front.shininess
            StateMaterialBackAmbient,               // state.material.back.ambient
            StateMaterialBackDiffuse,               // state.material.back.diffuse
            StateMaterialBackSpecular,              // state.material.back.specular
            StateMaterialBackEmission,              // state.material.back.emission
            StateMaterialBackShininess,             // state.material.back.shininess
            StateLightNAmbient,                     // state.light[n].ambient
            StateLightNDiffuse,                     // state.light[n].diffuse
            StateLightNSpecular,                    // state.light[n].specular
            StateLightNPosition,                    // state.light[n].position
            StateLightNShininess,                   // state.light[n].shininess
            StateLightNSpotDirection,               // state.light[n].spot.direction
            StateLightNHalf,                        // state.light[n].half
            StateLightModelAmbient,                 // state.lightmodel.ambient
            StateLightModelSceneColor,              // state.lightmodel.scenecolor
            StateLightModelFrontSceneColor,         // state.lightmodel.front.scenecolor
            StateLightModelBackSceneColor,          // state.lightmodel.back.scenecolor
            StateLightProdNAmbient,                 // state.lightprod[n].ambient
            StateLightProdNDiffuse,                 // state.lightprod[n].diffuse
            StateLightProdNSpecular,                // state.lightprod[n].specular
            StateLightProdNFrontAmbient,            // state.lightprod[n].front.ambient
            StateLightProdNFrontDiffuse,            // state.lightprod[n].front.diffuse
            StateLightProdNFrontSpecular,           // state.lightprod[n].front.specular
            StateLightProdNBackAmbient,             // state.lightprod[n].back.ambient
            StateLightProdNBackDiffuse,             // state.lightprod[n].back.diffuse
            StateLightProdNBackSpecular,            // state.lightprod[n].back.specular

            // Texture Bindings
            StateTexGenNEyeS,                       // state.texgen[n].eye.s
            StateTexGenNEyeT,                       // state.texgen[n].eye.t
            StateTexGenNEyeR,                       // state.texgen[n].eye.r
            StateTexGenNEyeQ,                       // state.texgen[n].eye.q
            StateTexGenNObjectS,                    // state.texgen[n].object.s
            StateTexGenNObjectT,                    // state.texgen[n].object.t
            StateTexGenNObjectR,                    // state.texgen[n].object.r
            StateTexGenNObjectQ,                    // state.texgen[n].object.q

            // Texture Enviroment Bindings
            StateTexEnvNColor,                      // state.texenv[n].color

            // Fog Bindings
            StateFogColor,                          // state.fog.color
            StateFogParams,                         // state.fog.params

            // Clip Plane Bindings
            StateClipNPlane,                        // state.clip[n].plane

            // Point Bindings
            StatePointSize,                         // state.point.size
            StatePointAttenuation,                  // state.point.attenuation

            // Depth Bindings
            StateDepthRange,                        // state.depth.range

            // Matrix Bindings
            StateMatrixModelViewN,                  // state.matrix.modelview[n]
            StateMatrixProjection,                  // state.matrix.projection
            StateMatrixMVP,                         // state.matrix.mvp
            StateMatrixTextureN,                    // state.matrix.texture[n]
            StateMatrixPaletteN,                    // state.matrix.palette[n]
            StateMatrixProgramN,                    // state.matrix.program[n]

            // Matrix Row Bindings
            StateMatrixModelViewNRowO,              // state.matrix.modelview[n].row[o]
            StateMatrixProjectionRowO,              // state.matrix.projection.row[o]
            StateMatrixMVPRowO,                     // state.matrix.mvp.row[o]
            StateMatrixTextureNRowO,                // state.matrix.texture[n].row[o]
            StateMatrixPaletteNRowO,                // state.matrix.palette[n].row[o]
            StateMatrixProgramNRowO,                // state.matrix.program[n].row[o]

            // Matrix Bindings Inverse
            StateMatrixInvModelViewN,               // state.matrix.modelview[n].inverse
            StateMatrixInvProjection,               // state.matrix.projection.inverse
            StateMatrixInvMVP,                      // state.matrix.mvp.inverse
            StateMatrixInvTextureN,                 // state.matrix.texture[n].inverse
            StateMatrixInvPaletteN,                 // state.matrix.palette[n].inverse
            StateMatrixInvProgramN,                 // state.matrix.program[n].inverse

            // Matrix Bindings Transpose
            StateMatrixTransModelViewN,             // state.matrix.modelview[n].transpose
            StateMatrixTransProjection,             // state.matrix.projection.transpose
            StateMatrixTransMVP,                    // state.matrix.mvp.transpose
            StateMatrixTransTextureN,               // state.matrix.texture[n].transpose
            StateMatrixTransPaletteN,               // state.matrix.palette[n].transpose
            StateMatrixTransProgramN,               // state.matrix.program[n].transpose

            // Matrix Bindings Inverse Transpose
            StateMatrixInvTransModelViewN,          // state.matrix.modelview[n].invtrans
            StateMatrixInvTransProjection,          // state.matrix.projection.invtrans
            StateMatrixInvTransMVP,                 // state.matrix.mvp.invtrans
            StateMatrixInvTransTextureN,            // state.matrix.texture[n].invtrans
            StateMatrixInvTransPaletteN,            // state.matrix.palette[n].invtrans
            StateMatrixInvTransProgramN,            // state.matrix.program[n].invtrans

            // Value
            Value,
            Vector2,
            Vector3,
            Vector4,
            Array,

            // Name
            Name,
        }
    }
}
